# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Directory.Packages.props ./
COPY ECM.sln ./
COPY src/Shared/ServiceDefaults/ServiceDefaults.csproj src/Shared/ServiceDefaults/
COPY src/ECM/ECM.BuildingBlocks/ECM.BuildingBlocks.csproj src/ECM/ECM.BuildingBlocks/
COPY src/Workers/OutboxDispatcher.Worker/OutboxDispatcher.csproj src/Workers/OutboxDispatcher.Worker/
RUN dotnet restore src/Workers/OutboxDispatcher.Worker/OutboxDispatcher.csproj

COPY src/ src/
RUN dotnet publish src/Workers/OutboxDispatcher.Worker/OutboxDispatcher.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "OutboxDispatcher.dll"]
