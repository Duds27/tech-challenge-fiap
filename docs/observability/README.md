# Observabilidade (New Relic)

Como a Oficina Mecânica é monitorada na Fase 3: **APM** (latência/erros/traces), **infra
Kubernetes** (CPU/memória/uptime), **logs estruturados** (JSON com `CorrelationId`) e
**dashboards + alertas** de negócio.

## O que é monitorado

| Requisito | Como |
| --- | --- |
| Latência das APIs | New Relic APM (agente .NET no container) + logs de requisição do Serilog. |
| Recursos do K8s (CPU/memória) | `nri-bundle` no cluster (provisionado pelo `infra-k8s`). |
| Healthchecks e uptime | Probes `/health/live` e `/health/ready` + Synthetics/health do APM. |
| Alertas de falha no processamento de OS | Log estruturado `Evento=os_falha_processamento` (nível Error). |
| Logs estruturados (JSON) + correlação | Serilog `CompactJsonFormatter` + `CorrelationId` (header `X-Correlation-ID`). |
| Volume diário de OS | Log `Evento=os_criada`. |
| Tempo médio por status | Log `Evento=os_status_alterado` (+ `DuracaoExecucaoSegundos`). |
| Erros/falhas em integrações | APM error rate + logs de erro. |

## Consultas NRQL (dashboards)

```sql
-- Volume diário de ordens de serviço
SELECT count(*) FROM Log WHERE Evento = 'os_criada' FACET dateOf(timestamp) SINCE 7 days ago

-- Tempo médio de execução (Em Execução → Finalizada), em minutos
SELECT average(DuracaoExecucaoSegundos)/60 AS 'min'
FROM Log WHERE Evento = 'os_status_alterado' AND StatusNovo = 'Finalizada' SINCE 1 day ago

-- Distribuição de transições por status
SELECT count(*) FROM Log WHERE Evento = 'os_status_alterado' FACET StatusNovo SINCE 1 day ago

-- Latência das APIs (APM)
SELECT percentile(duration, 50, 95, 99) FROM Transaction WHERE appName = 'oficina-mecanica-api' TIMESERIES

-- Erros / falhas no processamento de OS
SELECT count(*) FROM Log WHERE Evento = 'os_falha_processamento' SINCE 1 day ago TIMESERIES

-- CPU/memória dos pods
SELECT average(cpuUsedCores), average(memoryWorkingSetBytes)
FROM K8sContainerSample WHERE clusterName LIKE 'oficina-%' FACET podName TIMESERIES
```

> 💡 **No AWS Free Tier (t3.micro)** o `nri-bundle` não cabe (Helm estoura o timeout).
> Estratégia recomendada: **APM do app** para latência/erros/traces/logs + `kubectl top`
> para CPU/memória. Detalhes em [aws-setup.md §7.1](../aws-setup.md).

## Como aplicar

- **Dashboard:** importe [`newrelic-dashboard.json`](newrelic-dashboard.json) em
  _New Relic → Dashboards → Import_ (ajuste `accountId`).
- **Alertas:** provisionados por Terraform em [`terraform/`](terraform) (provider `newrelic`).
- **Correlação:** o app propaga `X-Correlation-ID`; o APM correlaciona logs ↔ traces
  automaticamente (application logging forwarding habilitado no Dockerfile).
