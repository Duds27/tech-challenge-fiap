# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY OficinaMecanicaBackend.sln ./
COPY src/OficinaMecanicaBackend/OficinaMecanicaBackend.csproj src/OficinaMecanicaBackend/
RUN dotnet restore src/OficinaMecanicaBackend/OficinaMecanicaBackend.csproj

COPY src/OficinaMecanicaBackend/ src/OficinaMecanicaBackend/
RUN dotnet publish src/OficinaMecanicaBackend/OficinaMecanicaBackend.csproj -c Release -o /publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /publish .

# Executa como usuário não-root 'app' (UID 1654, já presente nas imagens .NET 8).
# Cria o diretório de logs (usado pelo Serilog) e transfere a propriedade dos
# arquivos publicados para esse usuário, garantindo permissão de escrita em runtime.
RUN mkdir -p /app/logs && chown -R app:app /app
USER app

EXPOSE 8080
ENTRYPOINT ["dotnet", "OficinaMecanicaBackend.dll"]
