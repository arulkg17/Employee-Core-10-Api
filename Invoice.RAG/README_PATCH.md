# Invoice.RAG patch: Word (.docx) ingestion + multi-question tool agent

Copy these files over your project (same folders). New: Documents/*, Agent/*, Models/AgentModels.cs,
Controllers/AgentController.cs. Changed: Invoice.RAG.csproj, Program.cs, Configuration/RagOptions.cs,
Services/AgreementIngestionService.cs, Services/CoreApiClient.cs, Ollama/OllamaClient.cs, Pdf/TextChunker.cs,
Controllers/VendorAgreementsController.cs.

1. dotnet restore   (adds DocumentFormat.OpenXml)
2. Merge appsettings.additions.json into appsettings.json; fix the two CoreApiEndpoints routes from Swagger.
3. No SQL change is needed.
4. Run, then use Http/agent-and-docx.http. Test set: eval/sample-questions.json.

Not compiled here (no .NET SDK in my sandbox): build once and send me any compiler errors.
