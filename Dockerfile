# ==============================
# Estágio 1: Base - runtime leve
# ==============================
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

# ==============================
# Estágio 2: SDK para build
# ==============================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["OrionHealth.sln", "."]
COPY ["src/", "src/"]
COPY ["test/", "test/"]

# Restaura dependências
RUN dotnet restore "OrionHealth.sln"

# Copia tudo e build
COPY . .
RUN dotnet build "OrionHealth.sln" -c Release

# ==============================
# Estágio 3: Publish
# ==============================
FROM build AS publish
RUN dotnet publish "src/OrionHealth.Worker/OrionHealth.Worker.csproj" -c Release -o /app/publish/gateway
RUN dotnet publish "src/OrionHealth.Hl7Processor/OrionHealth.Hl7Processor.csproj" -c Release -o /app/publish/processor
RUN dotnet publish "src/OrionHealth.Notification/OrionHealth.Notification.csproj" -c Release -o /app/publish/notifier

# ==============================
# Imagens Finais para Produção
# ==============================
# Gateway (Worker)
FROM base AS gateway-final
WORKDIR /app
COPY --from=publish /app/publish/gateway .
ENTRYPOINT ["dotnet", "OrionHealth.Worker.dll"]

# Processor (Hl7Processor)
FROM base AS processor-final
WORKDIR /app
COPY --from=publish /app/publish/processor .
ENTRYPOINT ["dotnet", "OrionHealth.Hl7Processor.dll"]

# Notifier (Notification)
FROM base AS notifier-final
WORKDIR /app
COPY --from=publish /app/publish/notifier .
ENTRYPOINT ["dotnet", "OrionHealth.Notification.dll"]


