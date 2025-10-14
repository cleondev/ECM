# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Directory.Packages.props ./
COPY ECM.sln ./
COPY src/Aspire/ECM.ServiceDefaults/ServiceDefaults.csproj src/Aspire/ECM.ServiceDefaults/
COPY src/Modules/SearchIndexer/ECM.SearchIndexer.csproj src/Modules/SearchIndexer/
COPY src/Shared/Contracts/Shared.Contracts.csproj src/Shared/Contracts/
COPY src/Workers/SearchIndexer.Worker/SearchIndexer.csproj src/Workers/SearchIndexer.Worker/
RUN dotnet restore src/Workers/SearchIndexer.Worker/SearchIndexer.csproj

COPY src/ src/
RUN dotnet publish src/Workers/SearchIndexer.Worker/SearchIndexer.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "SearchIndexer.dll"]
