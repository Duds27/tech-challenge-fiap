# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY OficinaMecanicaBackend.sln ./
COPY src/OficinaMecanicaBackend/OficinaMecanicaBackend.csproj src/OficinaMecanicaBackend/
RUN dotnet restore src/OficinaMecanicaBackend/OficinaMecanicaBackend.csproj

COPY src/OficinaMecanicaBackend/ src/OficinaMecanicaBackend/
RUN dotnet publish src/OficinaMecanicaBackend/OficinaMecanicaBackend.csproj -c Release -o /publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "OficinaMecanicaBackend.dll"]
