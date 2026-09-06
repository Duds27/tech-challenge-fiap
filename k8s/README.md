# Manifestos Kubernetes — Oficina Mecânica

Deploy da aplicação (API .NET) + MySQL no namespace `oficina`, com escalabilidade
automática via HPA.

## Recursos

| Arquivo | Recurso | Descrição |
| ------- | ------- | --------- |
| `00-namespace.yaml` | Namespace `oficina` | Isola os recursos da aplicação. |
| `10-configmap.yaml` | ConfigMap `api-config` | Configuração não sensível (ambiente, issuer/audience JWT, nome do banco). |
| `11-secret.yaml` | Secret `api-secrets` | Segredos (connection string, chave JWT, senha do MySQL). **Valores de exemplo** — substitua em produção. |
| `20-mysql.yaml` | Service + StatefulSet `mysql` | MySQL 8.4 com volume persistente (`volumeClaimTemplates`) e Service headless (DNS `mysql`). |
| `30-api.yaml` | Deployment + Service `api` | 2 réplicas da API, `resources.requests/limits`, probes `/health/live` e `/health/ready`, config via `envFrom`. |
| `40-hpa.yaml` | HorizontalPodAutoscaler `api` | Escala de 2 a 10 réplicas por CPU (70%) e memória (80%). |

## Pré-requisitos

- Um cluster Kubernetes (localmente via **kind** — ver [`/infra`](../infra)).
- **metrics-server** instalado (necessário para o HPA coletar métricas de CPU/memória).
- Imagem `oficina-mecanica-api:local` disponível no cluster:
  ```bash
  docker build -t oficina-mecanica-api:local OficinaMecanicaBackend
  kind load docker-image oficina-mecanica-api:local --name oficina
  ```

## Aplicar

```bash
# Local/kind (MySQL in-cluster): overlay "local"
kubectl apply -k k8s/overlays/local
kubectl -n oficina rollout status deploy/api
kubectl -n oficina get pods,svc,hpa
```

> **Produção (AWS/EKS):** use o overlay `k8s/overlays/aws` (RDS gerenciado + NLB via
> TargetGroupBinding). O deploy é feito pelo pipeline do repositório `oficina-app`.

## Acessar a API localmente

```bash
kubectl -n oficina port-forward svc/api 8080:80
# Swagger: http://localhost:8080/swagger
# Health:  http://localhost:8080/health/ready
```

## Testar a escalabilidade (HPA)

Gere carga e observe o número de réplicas crescer:

```bash
kubectl -n oficina get hpa api -w
# em outro terminal, gere requisições (ex.: hey/ab/k6) contra o Service da API
```
