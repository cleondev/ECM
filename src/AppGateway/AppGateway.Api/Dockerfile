# ===== UI build (Next.js static export) =====
FROM node:20-bullseye AS ui
WORKDIR /src
COPY src/AppGateway/ui/package*.json ./
RUN npm ci
COPY src/AppGateway/ui/ .
# Build and export static site to /src/out
RUN npm run build && npm run export

# ===== API publish (.NET 9) =====
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/AppGateway/AppGateway.Api/AppGateway.Api.csproj \
 && dotnet publish src/AppGateway/AppGateway.Api/AppGateway.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# ===== Runtime (.NET 9 ASP.NET) =====
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish ./
# Copy exported UI into wwwroot
COPY --from=ui /src/out ./wwwroot
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "AppGateway.Api.dll"]