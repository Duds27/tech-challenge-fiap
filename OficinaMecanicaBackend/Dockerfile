# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copia os arquivos de projeto primeiro para aproveitar o cache de restore
COPY OficinaMecanicaBackend.sln ./
COPY src/OficinaMecanica.Domain/OficinaMecanica.Domain.csproj src/OficinaMecanica.Domain/
COPY src/OficinaMecanica.Application/OficinaMecanica.Application.csproj src/OficinaMecanica.Application/
COPY src/OficinaMecanica.Infrastructure/OficinaMecanica.Infrastructure.csproj src/OficinaMecanica.Infrastructure/
COPY src/OficinaMecanica.API/OficinaMecanica.API.csproj src/OficinaMecanica.API/
RUN dotnet restore src/OficinaMecanica.API/OficinaMecanica.API.csproj

# Copia o restante do código e publica
COPY src/ src/
RUN dotnet publish src/OficinaMecanica.API/OficinaMecanica.API.csproj -c Release -o /publish

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
ENTRYPOINT ["dotnet", "OficinaMecanica.API.dll"]
