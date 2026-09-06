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

# ─────────────────────────── New Relic .NET agent ───────────────────────────
# Instala o agente APM. A chave de licença e o nome da aplicação são fornecidos
# em runtime (env NEW_RELIC_LICENSE_KEY / NEW_RELIC_APP_NAME), a partir do Secret.
RUN apt-get update \
    && apt-get install -y --no-install-recommends wget ca-certificates gnupg \
    && wget -qO- https://download.newrelic.com/548C16BF.gpg | gpg --dearmor -o /usr/share/keyrings/newrelic.gpg \
    && echo "deb [signed-by=/usr/share/keyrings/newrelic.gpg] https://apt.newrelic.com/debian/ newrelic non-free" > /etc/apt/sources.list.d/newrelic.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends newrelic-dotnet-agent \
    && rm -rf /var/lib/apt/lists/*

ENV CORECLR_ENABLE_PROFILING=1 \
    CORECLR_PROFILER={36032161-FFC0-4B61-B559-F6C5D41BAE5A} \
    CORECLR_NEWRELIC_HOME=/usr/local/newrelic-dotnet-agent \
    CORECLR_PROFILER_PATH=/usr/local/newrelic-dotnet-agent/libNewRelicProfiler.so \
    NEW_RELIC_DISTRIBUTED_TRACING_ENABLED=true \
    NEW_RELIC_APPLICATION_LOGGING_FORWARDING_ENABLED=true

# Executa como usuário não-root 'app' (UID 1654, já presente nas imagens .NET 8).
# Cria o diretório de logs (usado pelo Serilog) e transfere a propriedade dos
# arquivos publicados para esse usuário, garantindo permissão de escrita em runtime.
RUN mkdir -p /app/logs && chown -R app:app /app
USER app

EXPOSE 8080
ENTRYPOINT ["dotnet", "OficinaMecanica.API.dll"]
