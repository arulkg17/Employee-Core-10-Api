# Invoice.AI - Single & Multi Intent questions (Ollama + Qwen2.5)

Ask business questions in plain English about **Category, ItemMaster, Customer, Vendor, User**:

* Single intent: "How many active items are in Rice?"
* Multi intent : "How many active customers and inactive vendors are there?"

## How it works (teach this diagram)

```
 question ──► [1] LLM (Qwen2.5)  ──► JSON intents      "understand"      (Ollama /api/chat + JSON schema)
              [2] C# validation  ──► fix spelling, active/inactive, unknown intents
              [3] C# handler     ──► COUNT query        "facts"           (IMasterDataQueryService)
              [4] C# sentence    ──► "There are 4 active customers."
```

Why this design (instead of Semantic Kernel function calling):

* A 7B local model is unreliable at *choosing and chaining tools*, but very good at
  *turning a sentence into JSON*. The JSON schema in Ollama's `format` field forces valid output.
* Only ONE LLM call per request (no second call to "write the answer") -> faster on a laptop.
* The LLM never sees database data and never writes numbers -> it cannot invent a count.
* Wrong "active"/"inactive" from the model is corrected in C# (`ActiveFilterResolver`).
* Typos ("Vegetables" vs "Vegitables") are corrected in C# (`FuzzyMatcher`).
* Single and Multi use the same pipeline - only the prompt rule differs
  ("exactly one" vs "one per question").

## Projects

```
Invoice.AI/                  <- NEW class library (net8.0)
  Abstractions/              interfaces (orchestrator, ollama client, handler, data service)
  Models/                    AskRequest, ExtractedIntent, IntentArguments, AIAnswer ...
  Configuration/             InvoiceAIOptions, OllamaOptions
  Ollama/OllamaClient.cs     HTTP call to Ollama
  Intents/                   IntentNames, IntentCatalog, IntentPromptBuilder, IntentExtractor
  Intents/Handlers/          one file per master (Category, Item, Customer, Vendor, User)
  Services/                  AIOrchestrator (read first!), ActiveFilterResolver, FuzzyMatcher
  Data/                      MasterTableMap, SqlMasterDataQueryService, InMemoryMasterDataQueryService
  ServiceCollectionExtensions.cs   services.AddInvoiceAI(configuration)
Invoice.AI.Tests/            xUnit tests (no Ollama / no SQL Server needed)
Invoice.Coreapi/             files to copy INTO your API project
  Controllers/InvoiceAIController.cs
  appsettings.InvoiceAI.snippet.json
  Program.snippet.txt
```

## Setup

1. **Ollama**
   ```
   ollama pull qwen2.5:7b-instruct
   ollama serve        (skip if it already runs as a service)
   ```
2. **Add projects to the solution** (run in the solution folder, after copying the folders in)
   ```
   dotnet sln add Invoice.AI/Invoice.AI.csproj
   dotnet sln add Invoice.AI.Tests/Invoice.AI.Tests.csproj
   dotnet add Invoice.Coreapi/Invoice.Coreapi.csproj reference Invoice.AI/Invoice.AI.csproj
   ```
   (If your solution targets .NET 10, you may change `net8.0` to `net10.0` in both csproj files.)
3. **Copy** `Controllers/InvoiceAIController.cs` into your API project
   (fix the namespace if your root namespace is different).
4. **Program.cs** - add `using Invoice.AI;` and `builder.Services.AddInvoiceAI(builder.Configuration);`
5. **appsettings.json** - paste the `InvoiceAI` section from `appsettings.InvoiceAI.snippet.json`.
   * First run: keep `"UseInMemoryData": true` (fake data, no database needed).
   * Then set it to `false` to use SQL Server (see "Real database" below).
6. Run the unit tests first: `dotnet test Invoice.AI.Tests`  (about 20 tests, no Ollama needed).

## Test in Swagger / Postman

Endpoints (route `api/invoice-ai` so it does not clash with your old `api/ai/*`):

| Method | URL | Purpose |
|---|---|---|
| POST | `/api/invoice-ai/ask` | single intent |
| POST | `/api/invoice-ai/ask-multi` | one or many intents |
| GET  | `/api/invoice-ai/intents` | what the AI understands |

Body: `{ "question": "How many active items are in Rice?" }`

### Expected answers with `UseInMemoryData: true`

| Endpoint | Question | Expected answer |
|---|---|---|
| ask | How many items are in Rice? | There are 3 items in the Rice category. |
| ask | How many active items are in Rice? | There are 2 active items in the Rice category. |
| ask | How many inactive items are in Rice? | There is 1 inactive item in the Rice category. |
| ask | How many items are in Vegetables? (typo) | There are 3 items in the Vegitables category. |
| ask | How many items are there in total? | There are 14 items. |
| ask | How many categories are there? | There are 7 categories. |
| ask | How many active customers are there? | There are 4 active customers. |
| ask | How many customers are in Chennai? | There are 3 customers in Chennai. |
| ask | Show customer count for every city | Customers by city: Chennai 3, Mumbai 2, Pune 1. |
| ask | How many inactive vendors are there? | There is 1 inactive vendor. |
| ask | How many active users are there? | There are 4 active users. |
| ask | What is the weather today? | I don't currently support that type of business question. |
| ask-multi | How many active customers and inactive vendors are there? | There are 4 active customers. There is 1 inactive vendor. |
| ask-multi | How many items are in Oil, how many users and how many categories? | There are 2 items in the Oil category. There are 5 users. There are 7 categories. |
| ask-multi | How many active customers in Chennai and how many vendors? | There are 2 active customers in Chennai. There are 4 vendors. |
| ask | How many customers and vendors are there? | first answer + note that only the first question was answered |

With `IncludeDebugInfo: true` the response contains `debug.rawModelOutput` and `debug.extractedIntents`
so students can SEE what the LLM understood versus what C# finally did.

## Real database

Open `Invoice.AI/Data/MasterTableMap.cs` and make the names match your tables
(defaults: `Category`, `ItemMaster`, `Customer`, `Vendor`, `Users`, column `IsActive`,
`ItemMaster.CategoryId` -> `Category.Id` as INT, `Customer.City`).
Then set `"UseInMemoryData": false`. It reuses the `IDbConnection` already registered in your Program.cs.

## Add a new question type (e.g. "How many items use UOM Kg?")

1. Add a method to `IMasterDataQueryService` + implement it in the SQL and in-memory services.
2. Add an `IntentNames` constant and a handler class (copy `VendorHandlers.cs`).
3. Add one `services.AddScoped<IIntentHandler, ...>()` line in `ServiceCollectionExtensions`.

The prompt and JSON schema are generated from the registered handlers - no prompt editing needed.
Add 1-2 examples to `IntentPromptBuilder` if the small model still mixes it up with another intent.

## Troubleshooting

| Symptom | Fix |
|---|---|
| 503 "AI model is not reachable" | start Ollama; `BaseUrl` must be `http://localhost:11434` (no `/v1`) |
| 504 timeout | first call loads the model - wait, or raise `TimeoutSeconds`; `KeepAlive` keeps it loaded afterwards |
| Wrong intent chosen | look at `debug.rawModelOutput`; improve the handler `Description` or add an example |
| "could not find the category" | check `MasterTableMap` and the category spelling in the database |
| Slow on CPU | try `qwen2.5:3b-instruct` (less accurate) or close other apps; context is already small (`NumCtx` 4096) |


   ollama pull qwen2.5:3b-instruct
   ollama run qwen2.5:3b-instruct --verbose "Say hello in five words"
