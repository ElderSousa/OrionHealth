# Estágio 1: Base - A mesma base para todos os serviços
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

# Estágio 2: Build - Compila toda a solução de uma só vez
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["OrionHealth.sln", "."]
COPY ["src/", "src/"]
COPY ["test/", "test/"]
RUN dotnet restore "OrionHealth.sln"
COPY . .
WORKDIR "/src"
RUN dotnet build "OrionHealth.sln" -c Release

# Estágio 3: Publish - Publica as três aplicações
FROM build AS publish
RUN dotnet publish "src/OrionHealth.Worker/OrionHealth.Worker.csproj" -c Release -o /app/publish/gateway
RUN dotnet publish "src/OrionHealth.WorkerHl7Processor/OrionHealth.WorkerHl7Processor.csproj" -c Release -o /app/publish/processor
RUN dotnet publish "src/OrionHealth.Notifier/OrionHealth.Notifier.csproj" -c Release -o /app/publish/notifier

# --- Imagens Finais ---

# Imagem final para o Gateway (Worker)
FROM base AS gateway-final
WORKDIR /app
COPY --from=publish /app/publish/gateway .
ENTRYPOINT ["dotnet", "OrionHealth.Worker.dll"]

# Imagem final para o Processador (Hl7Processor)
FROM base AS processor-final
WORKDIR /app
COPY --from=publish /app/publish/processor .
ENTRYPOINT ["dotnet", "OrionHealth.WorkerHl7Processor.dll"]

# Imagem final para o Notificador (Notifier)
FROM base AS notifier-final
WORKDIR /app
COPY --from=publish /app/publish/notifier .
ENTRYPOINT ["dotnet", "OrionHealth.Notifier.dll"]
