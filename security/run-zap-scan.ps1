#!/usr/bin/env pwsh
# ============================================================
# Script: run-zap-scan.ps1
# Executa varredura DAST com OWASP ZAP contra a API em execução.
#
# Pré-requisitos:
#   1. Docker Desktop em execução
#   2. API rodando: cd OficinaMecanicaBackend && docker compose up
#   3. A API deve estar acessível em http://localhost:8080
#
# Uso:
#   .\security\run-zap-scan.ps1
#   .\security\run-zap-scan.ps1 -TargetUrl "http://localhost:8080" -ScanType Full
# ============================================================

param(
    [string]$TargetUrl  = "http://localhost:8080",
    [string]$ScanType   = "Baseline",   # Baseline | Full
    [string]$ReportDir  = "$PSScriptRoot\reports"
)

$ErrorActionPreference = "Stop"

Write-Host "`n=== OWASP ZAP DAST Scan — OficinaMecanicaBackend ===" -ForegroundColor Cyan
Write-Host "Target  : $TargetUrl" -ForegroundColor Yellow
Write-Host "Tipo    : $ScanType" -ForegroundColor Yellow
Write-Host "Reports : $ReportDir`n" -ForegroundColor Yellow

# Cria pasta de relatórios
if (-not (Test-Path $ReportDir)) {
    New-Item -ItemType Directory -Path $ReportDir | Out-Null
}

# ── Verifica disponibilidade da API ──────────────────────────
Write-Host "[1/3] Verificando disponibilidade da API..." -ForegroundColor Green
try {
    $null = Invoke-WebRequest -Uri "$TargetUrl/api/auth/login" `
        -Method POST -ContentType "application/json" `
        -Body '{"username":"x","password":"x"}' `
        -TimeoutSec 10 -ErrorAction Stop
} catch {
    if ($_.Exception.Response -and $_.Exception.Response.StatusCode.value__ -in @(400, 401)) {
        Write-Host "API disponível (respondeu com $($_.Exception.Response.StatusCode))." -ForegroundColor Green
    } else {
        Write-Error "API nao acessivel em $TargetUrl. Inicie a aplicacao antes de executar o scan.`nErro: $_"
    }
}

# ── Monta argumentos ZAP ─────────────────────────────────────
$ZapImage = "ghcr.io/zaproxy/zaproxy:stable"
$ReportBase = "zap-$($ScanType.ToLower())-report"

if ($ScanType -eq "Full") {
    $ZapCmd = "zap-full-scan.py"
} else {
    $ZapCmd = "zap-baseline.py"
}

# Converte path Windows → Docker volume path
$DockerReportDir = $ReportDir -replace "\\", "/"
if ($DockerReportDir -match "^([A-Za-z]):") {
    $DockerReportDir = "/" + $Matches[1].ToLower() + $DockerReportDir.Substring(2)
}

# ── Executa scan ─────────────────────────────────────────────
Write-Host "[2/3] Executando ZAP $ScanType Scan..." -ForegroundColor Green

docker run --rm `
    --network host `
    -v "${DockerReportDir}:/zap/wrk:rw" `
    $ZapImage `
    $ZapCmd `
    -t $TargetUrl `
    -r "$ReportBase.html" `
    -J "$ReportBase.json" `
    -x "$ReportBase.xml" `
    -I

$ExitCode = $LASTEXITCODE
if ($ExitCode -gt 2) {
    Write-Warning "ZAP retornou codigo $ExitCode (alertas encontrados — verifique o relatorio)."
}

# ── Finaliza ─────────────────────────────────────────────────
Write-Host "`n[3/3] Relatórios salvos em: $ReportDir" -ForegroundColor Green
Write-Host "  - $ReportBase.html  (legível no navegador)" -ForegroundColor White
Write-Host "  - $ReportBase.json  (análise programática)" -ForegroundColor White
Write-Host "  - $ReportBase.xml   (integração CI/CD)" -ForegroundColor White
Write-Host "`n=== Scan ZAP concluído! ===" -ForegroundColor Cyan
