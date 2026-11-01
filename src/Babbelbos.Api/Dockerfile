# Build from the repository root:
#   docker build -f src/Babbelbos.Api/Dockerfile -t babbelbos-api .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

# Restore first, so this layer is cached as long as the project files do not change.
COPY global.json nuget.config Directory.Build.props Directory.Packages.props ./
COPY src/Babbelbos.Api/Babbelbos.Api.csproj src/Babbelbos.Api/
COPY src/Babbelbos.ServiceDefaults/Babbelbos.ServiceDefaults.csproj src/Babbelbos.ServiceDefaults/
COPY src/BuildingBlocks/BuildingBlocks.Domain/BuildingBlocks.Domain.csproj src/BuildingBlocks/BuildingBlocks.Domain/
COPY src/BuildingBlocks/BuildingBlocks.Application/BuildingBlocks.Application.csproj src/BuildingBlocks/BuildingBlocks.Application/
COPY src/Modules/ src/Modules/
RUN dotnet restore src/Babbelbos.Api/Babbelbos.Api.csproj

COPY src/ src/
COPY prompts/ prompts/
RUN dotnet publish src/Babbelbos.Api/Babbelbos.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app

# Chiseled image: no shell, no package manager, runs as a non-root user.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Babbelbos.Api.dll"]
