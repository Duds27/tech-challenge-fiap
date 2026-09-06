# ADR-002 — Escalabilidade com HPA (Horizontal Pod Autoscaler)

- **Status:** Aceito
- **Data:** 2026-09-06

## Contexto

Com múltiplas unidades e mais clientes, a API precisa escalar horizontalmente conforme a carga,
sem intervenção manual, mantindo disponibilidade.

## Decisão

- Usar **HPA** no Deployment da API (2 a N réplicas) com alvos de **CPU 70% / memória 80%**,
  baseado em `resources.requests` definidos no manifesto.
- Instalar **metrics-server** no cluster (fonte de métricas do HPA).
- Complementar com **Cluster Autoscaler** no managed node group do EKS, para adicionar nós quando
  os pods não couberem.

## Consequências

- **Positivas:** resposta automática a picos (ex.: campanhas/manhãs de segunda), custo elástico,
  resiliência a falha de pod.
- **Negativas / mitigação:** requer `requests` bem calibrados — validar com teste de carga
  (`k8s/loadtest`) e ajustar limites; HPA depende do metrics-server saudável (monitorado no New Relic).
