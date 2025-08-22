# Estágio 1: Base - Define a base comum para os nossos serviços
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

# Estágio 2: Build - Compila toda a solução
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["OrionHealth.sln", "."]
COPY ["src/OrionHealth.Application/OrionHealth.Application.csproj", "src/OrionHealth.Application/"]
COPY ["src/OrionHealth.CrossCutting/OrionHealth.CrossCutting.csproj", "src/OrionHealth.CrossCutting/"]
COPY ["src/OrionHealth.Domain/OrionHealth.Domain.csproj", "src/OrionHealth.Domain/"]
COPY ["src/OrionHealth.Infrastructure/OrionHealth.Infrastructure.csproj", "src/OrionHealth.Infrastructure/"]
COPY ["src/OrionHealth.Worker/OrionHealth.Worker.csproj", "src/OrionHealth.Worker/"]
COPY ["src/OrionHealth.Hl7Processor/OrionHealth.Hl7Processor.csproj", "src/OrionHealth.Hl7Processor/"]
RUN dotnet restore "OrionHealth.sln"

COPY . .
WORKDIR "/src/src"
RUN dotnet build "OrionHealth.Worker/OrionHealth.Worker.csproj" -c Release -o /app/build/worker
RUN dotnet build "OrionHealth.Hl7Processor/OrionHealth.Hl7Processor.csproj" -c Release -o /app/build/processor

# Estágio 3: Publish - Publica as duas aplicações
FROM build AS publish
RUN dotnet publish "OrionHealth.Worker/OrionHealth.Worker.csproj" -c Release -o /app/publish/worker
RUN dotnet publish "OrionHealth.Hl7Processor/OrionHealth.Hl7Processor.csproj" -c Release -o /app/publish/processor

# Estágio Final 1: Imagem final para o Gateway (Worker)
FROM base AS gateway-final
WORKDIR /app
COPY --from=publish /app/publish/worker .
ENTRYPOINT ["dotnet", "OrionHealth.Worker.dll"]

# Estágio Final 2: Imagem final para o Processador (Hl7Processor)
FROM base AS processor-final
WORKDIR /app
COPY --from=publish /app/publish/processor .
ENTRYPOINT ["dotnet", "OrionHealth.Hl7Processor.dll"]
