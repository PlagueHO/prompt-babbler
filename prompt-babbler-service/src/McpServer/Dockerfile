FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS base

WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY src/McpServer/PromptBabbler.McpServer.csproj src/McpServer/
COPY src/Domain/PromptBabbler.Domain.csproj src/Domain/
COPY src/Orchestration/ServiceDefaults/PromptBabbler.ServiceDefaults.csproj src/Orchestration/ServiceDefaults/
RUN dotnet restore src/McpServer/PromptBabbler.McpServer.csproj

COPY src/ src/
WORKDIR /src/src/McpServer
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PromptBabbler.McpServer.dll"]
