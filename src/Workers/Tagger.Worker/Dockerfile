# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Directory.Packages.props ./
COPY ECM.sln ./
COPY src/Workers/Tagger.Worker/Tagger.csproj src/Workers/Tagger.Worker/
COPY src/Workers/Shared/Workers.Shared.csproj src/Workers/Shared/
COPY src/Shared/ServiceDefaults/ServiceDefaults.csproj src/Shared/ServiceDefaults/
COPY src/Shared/Contracts/Shared.Contracts.csproj src/Shared/Contracts/
COPY src/Shared/Extensions/Shared.Extensions.csproj src/Shared/Extensions/
COPY src/Shared/Utilities/Shared.Utilities.csproj src/Shared/Utilities/
COPY src/Modules/Document/ECM.Document.csproj src/Modules/Document/
COPY src/Modules/Abstractions/ECM.Abstractions.csproj src/Modules/Abstractions/
COPY src/Modules/Operations/ECM.Operations.csproj src/Modules/Operations/
COPY src/ECM/ECM.BuildingBlocks/ECM.BuildingBlocks.csproj src/ECM/ECM.BuildingBlocks/
RUN dotnet restore src/Workers/Tagger.Worker/Tagger.csproj

COPY src/ src/
RUN dotnet publish src/Workers/Tagger.Worker/Tagger.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "Tagger.dll"]
