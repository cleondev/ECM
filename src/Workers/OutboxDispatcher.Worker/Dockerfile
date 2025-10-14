FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/ECM/ECM.Host/ECM.Host.csproj \
 && dotnet publish  src/ECM/ECM.Host/ECM.Host.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/Workers/OutboxDispatcher.Worker/OutboxDispatcher.csproj \
 && dotnet publish  src/Workers/OutboxDispatcher.Worker/OutboxDispatcher.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "OutboxDispatcher.dll"]
WORKDIR /app
COPY --from=build /app/publish ./
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ECM.Host.dll"]