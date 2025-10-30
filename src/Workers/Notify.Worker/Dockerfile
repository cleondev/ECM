# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Directory.Packages.props ./
COPY ECM.sln ./
COPY src/Shared/ServiceDefaults/ServiceDefaults.csproj src/Shared/ServiceDefaults/
COPY src/Workers/Notify.Worker/Notify.csproj src/Workers/Notify.Worker/
RUN dotnet restore src/Workers/Notify.Worker/Notify.csproj

COPY src/ src/
RUN dotnet publish src/Workers/Notify.Worker/Notify.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "Notify.dll"]
