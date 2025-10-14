# syntax=docker/dockerfile:1

# ===== UI build (Next.js static export) =====
FROM node:20-bullseye AS ui
WORKDIR /src

COPY src/AppGateway/ui/package*.json ./
RUN npm ci

COPY src/AppGateway/ui/ ./
RUN npm run build

# ===== API publish (.NET 9) =====
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Directory.Packages.props ./
COPY ECM.sln ./
COPY src/Aspire/ECM.ServiceDefaults/ServiceDefaults.csproj src/Aspire/ECM.ServiceDefaults/
COPY src/AppGateway/AppGateway.Api/AppGateway.Api.csproj src/AppGateway/AppGateway.Api/
COPY src/AppGateway/AppGateway.Application/AppGateway.Application.csproj src/AppGateway/AppGateway.Application/
COPY src/AppGateway/AppGateway.Infrastructure/AppGateway.Infrastructure.csproj src/AppGateway/AppGateway.Infrastructure/
COPY src/AppGateway/AppGateway.Contracts/AppGateway.Contracts.csproj src/AppGateway/AppGateway.Contracts/
RUN dotnet restore src/AppGateway/AppGateway.Api/AppGateway.Api.csproj

COPY src/ src/
RUN dotnet publish src/AppGateway/AppGateway.Api/AppGateway.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# ===== Runtime (.NET 9 ASP.NET) =====
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./
# Copy the static export (Next.js writes to dist per next.config)
COPY --from=ui /src/dist ./wwwroot

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AppGateway.Api.dll"]
