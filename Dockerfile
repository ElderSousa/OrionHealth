FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

COPY src/OrionHealth.Application/OrionHealth.Application.csproj src/OrionHealth.Application/
COPY src/OrionHealth.CrossCutting/OrionHealth.CrossCutting.csproj src/OrionHealth.CrossCutting/
COPY src/OrionHealth.Domain/OrionHealth.Domain.csproj src/OrionHealth.Domain/
COPY src/OrionHealth.Infrastructure/OrionHealth.Infrastructure.csproj src/OrionHealth.Infrastructure/
COPY src/OrionHealth.Worker/OrionHealth.Worker.csproj src/OrionHealth.Worker/

RUN dotnet restore src/OrionHealth.Worker/OrionHealth.Worker.csproj

COPY . .

RUN dotnet publish -c Release -o out src/OrionHealth.Worker/OrionHealth.Worker.csproj


FROM mcr.microsoft.com/dotnet/runtime:8.0

WORKDIR /app

COPY --from=build /app/out .

ENTRYPOINT ["dotnet", "OrionHealth.Worker.dll"]