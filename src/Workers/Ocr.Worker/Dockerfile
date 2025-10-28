# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Packages.props", "."]
COPY ["src/Workers/Ocr.Worker/Ocr.csproj", "src/Workers/Ocr.Worker/"]
COPY ["src/Shared/ServiceDefaults/ServiceDefaults.csproj", "src/Shared/ServiceDefaults/"]
COPY ["src/ECM/ECM.BuildingBlocks/ECM.BuildingBlocks.csproj", "src/ECM/ECM.BuildingBlocks/"]
COPY ["src/Modules/Ocr/ECM.Ocr.csproj", "src/Modules/Ocr/"]
COPY ["src/Modules/Abstractions/ECM.Abstractions.csproj", "src/Modules/Abstractions/"]
COPY ["src/Modules/Document/ECM.Document.csproj", "src/Modules/Document/"]
COPY ["src/Modules/Operations/ECM.Operations.csproj", "src/Modules/Operations/"]
COPY ["src/Shared/Extensions/Shared.Extensions.csproj", "src/Shared/Extensions/"]
COPY ["src/Shared/Utilities/Shared.Utilities.csproj", "src/Shared/Utilities/"]
COPY ["src/Shared/Contracts/Shared.Contracts.csproj", "src/Shared/Contracts/"]
COPY ["src/Modules/File/ECM.File.csproj", "src/Modules/File/"]
COPY ["src/Workers/Shared/Workers.Shared.csproj", "src/Workers/Shared/"]
RUN dotnet restore "./src/Workers/Ocr.Worker/Ocr.csproj"
COPY . .
WORKDIR "/src/src/Workers/Ocr.Worker"
RUN dotnet build "./Ocr.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Ocr.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ocr.dll"]