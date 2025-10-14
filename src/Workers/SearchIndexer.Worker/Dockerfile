FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/Workers/SearchIndexer.Worker/SearchIndexer.csproj \
 && dotnet publish  src/Workers/SearchIndexer.Worker/SearchIndexer.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "SearchIndexer.dll"]