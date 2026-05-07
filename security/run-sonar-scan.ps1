#!/usr/bin/env pwsh
# ============================================================
# Script: run-sonar-scan.ps1
# Executa análise SAST com SonarQube no projeto OficinaMecanicaBackend.
#
# Pré-requisitos:
#   1. SonarQube rodando: docker compose -f security/docker-compose.security.yml up sonarqube
#   2. dotnet-sonarscanner instalado (instalado automaticamente abaixo)
#   3. Token gerado em http://localhost:9000 > My Account > Security
#
# Uso:
#   .\security\run-sonar-scan.ps1 -SonarToken "sqp_xxxxxxxxxxxxxxxx"
#   ou defina: $env:SONAR_TOKEN = "sqp_xxx"
# ============================================================

param(
    [string]$SonarToken   = $env:SONAR_TOKEN,
    [string]$SonarHost    = "http://localhost:9000",
    [string]$ProjectKey   = "oficina-mecanica-backend"
)

$ErrorActionPreference = "Stop"

$Root        = Split-Path -Parent $PSScriptRoot
$BackendRoot = Join-Path $Root "OficinaMecanicaBackend"

Write-Host "`n=== SonarQube Security Scan — OficinaMecanicaBackend ===" -ForegroundColor Cyan

if (-not $SonarToken) {
    Write-Error "SONAR_TOKEN não definido. Gere um token em $SonarHost e passe via -SonarToken ou variável de ambiente SONAR_TOKEN."
}

# Verifica se dotnet-sonarscanner está instalado
if (-not (Get-Command dotnet-sonarscanner -ErrorAction SilentlyContinue)) {
    Write-Host "Instalando dotnet-sonarscanner..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-sonarscanner
    $env:PATH += ";$env:USERPROFILE\.dotnet\tools"
}

Push-Location $BackendRoot

try {
    Write-Host "`n[1/4] Iniciando análise no SonarQube ($SonarHost)..." -ForegroundColor Green
    dotnet sonarscanner begin `
        /k:"$ProjectKey" `
        /n:"Oficina Mecanica Backend" `
        /d:sonar.host.url="$SonarHost" `
        /d:sonar.token="$SonarToken" `
        /d:sonar.cs.opencover.reportsPaths="tests/OficinaMecanicaBackend.Tests/**/coverage.opencover.xml" `
        /d:sonar.exclusions="**/Migrations/**,**/obj/**,**/bin/**" `
        /d:sonar.coverage.exclusions="**/Migrations/**,**/Program.cs" `
        /d:sonar.qualitygate.wait=true

    Write-Host "`n[2/4] Compilando solução..." -ForegroundColor Green
    dotnet build OficinaMecanicaBackend.sln --no-incremental -c Release

    Write-Host "`n[3/4] Executando testes com cobertura (OpenCover)..." -ForegroundColor Green
    dotnet test OficinaMecanicaBackend.sln `
        --collect:"XPlat Code Coverage" `
        --results-directory ./TestResults `
        -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover

    Write-Host "`n[4/4] Enviando resultados para o SonarQube..." -ForegroundColor Green
    dotnet sonarscanner end /d:sonar.token="$SonarToken"

    Write-Host "`n=== Análise concluída com sucesso! ===" -ForegroundColor Cyan
    Write-Host "Acesse o relatório em: $SonarHost/dashboard?id=$ProjectKey" -ForegroundColor Yellow
}
finally {
    Pop-Location
}
