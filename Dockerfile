FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY . .
RUN dotnet publish -c Release -o out src/OrionHealth.Worker/OrionHealth.Worker.csproj

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app

# --- INÍCIO DA CORREÇÃO ---
# Instala o netcat (ferramenta de rede) e copia o nosso script de espera
RUN apt-get update && apt-get install -y netcat-openbsd && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/wait-for-db.sh .
RUN chmod +x ./wait-for-db.sh
# --- FIM DA CORREÇÃO ---

COPY --from=build /app/out .

# --- MUDANÇA FINAL ---
# O Entrypoint agora é o nosso script, que irá esperar pelo oracle-db
# e só depois executar "dotnet OrionHealth.Worker.dll"
ENTRYPOINT ["./wait-for-db.sh", "oracle-db", "dotnet", "OrionHealth.Worker.dll"]