# syntax=docker/dockerfile:1

# --- Build ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiamos el código del backend (los csproj de la API referencian Application/
# Infrastructure/Domain, todos bajo src/) y restauramos solo la API.
COPY src/ ./src/
RUN dotnet restore src/GymFlow.Api/GymFlow.Api.csproj

RUN dotnet publish src/GymFlow.Api/GymFlow.Api.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

# --- Runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish ./

ENV ASPNETCORE_ENVIRONMENT=Production
# El puerto real lo inyecta Render vía la variable PORT (lo lee Program.cs).
EXPOSE 10000

ENTRYPOINT ["dotnet", "GymFlow.Api.dll"]
