# syntax=docker/dockerfile:1

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files first for better layer caching
COPY Invoice.CoreAPI/Invoice.CoreAPI.csproj Invoice.CoreAPI/
COPY Invoice.BAL/Invoice.BAL.csproj Invoice.BAL/
COPY Invoice.DAL/Invoice.DAL.csproj Invoice.DAL/
COPY Invoice.Data/Invoice.Data.csproj Invoice.Data/
COPY Invoice.Model/Invoice.Model.csproj Invoice.Model/
COPY Invoice.DTOs/Invoice.DTOs.csproj Invoice.DTOs/

RUN dotnet restore Invoice.CoreAPI/Invoice.CoreAPI.csproj

# Copy the rest and publish (obj/bin are excluded via .dockerignore,
# so this always resolves fresh for the Linux target)
COPY . .
RUN dotnet publish Invoice.CoreAPI/Invoice.CoreAPI.csproj \
    -c Release \
    -o /app/publish

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# .NET 8+ container images default to port 8080 for HTTP
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

COPY --from=build /app/publish .

# Basic container health check hitting the app itself
HEALTHCHECK --interval=15s --timeout=5s --start-period=20s --retries=5 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Invoice.CoreAPI.dll"]
