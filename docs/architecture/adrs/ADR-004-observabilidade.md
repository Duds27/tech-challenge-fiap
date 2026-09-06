# ADR-004 — Observabilidade com New Relic

- **Status:** Aceito
- **Data:** 2026-09-06

## Contexto

É preciso visibilidade total: latência das APIs, consumo de CPU/memória do K8s, healthchecks/uptime,
alertas para falhas no processamento de OS, logs estruturados (JSON) com correlação, e dashboards
de negócio (volume diário de OS, tempo médio por status, erros de integração).

## Decisão

Adotar **New Relic** como plataforma única de observabilidade:

- **APM .NET** no container da API (auto-instrumentação) — latência, throughput, erros, traces distribuídos.
- **Integração Kubernetes** via Helm `nri-bundle` — CPU/memória/uptime dos pods e nós.
- **Logs** estruturados em JSON (Serilog `CompactJsonFormatter`) com `CorrelationId`, encaminhados ao New Relic.
- **Dashboards (NRQL)** e **alertas** (latência, uptime, recursos, falha de OS).

## Consequências

- **Positivas:** APM + infra + logs + alertas num só lugar; free tier generoso (adequado ao projeto);
  correlação ponta a ponta via `CorrelationId` + trace context.
- **Negativas / mitigação:** dependência de licença (chave no Secret) e de agente no container —
  chave via Secrets Manager/variável de ambiente; agente adiciona leve overhead, aceitável.
- **Alternativas descartadas:** Datadog (free tier mais restrito para APM); Grafana+Prometheus
  (mais setup/manutenção; o enunciado sugere Datadog/New Relic).
