# syntax=docker/dockerfile:1

# ===== UI build (Next.js static export) =====
FROM node:20-bullseye AS ui
WORKDIR /src

ARG NEXT_PUBLIC_SYNCFUSION_KEY
ENV NEXT_PUBLIC_SYNCFUSION_KEY=$NEXT_PUBLIC_SYNCFUSION_KEY

COPY src/AppGateway/ui/package*.json ./
RUN npm ci

COPY src/AppGateway/ui/ ./

RUN node -e "console.log('SYNCFUSION KEY LENGTH:', (process.env.NEXT_PUBLIC_SYNCFUSION_KEY || '').length)"

RUN npm run build


# ===== API publish (.NET 9) =====
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Directory.Packages.props ./
COPY ECM.sln ./
COPY src/Shared/ServiceDefaults/ServiceDefaults.csproj src/Shared/ServiceDefaults/
COPY src/AppGateway/AppGateway.Api/AppGateway.Api.csproj src/AppGateway/AppGateway.Api/
COPY src/AppGateway/AppGateway.Infrastructure/AppGateway.Infrastructure.csproj src/AppGateway/AppGateway.Infrastructure/
COPY src/AppGateway/AppGateway.Contracts/AppGateway.Contracts.csproj src/AppGateway/AppGateway.Contracts/
RUN dotnet restore src/AppGateway/AppGateway.Api/AppGateway.Api.csproj

COPY src/ src/
RUN dotnet publish src/AppGateway/AppGateway.Api/AppGateway.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# ===== Runtime (.NET 9 ASP.NET) =====
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./
COPY --from=ui /src/dist ./wwwroot

#ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080
EXPOSE 8443

ENTRYPOINT ["dotnet", "AppGateway.Api.dll"]
