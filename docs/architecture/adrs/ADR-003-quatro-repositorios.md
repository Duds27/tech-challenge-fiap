# ADR-003 — Segregação em 4 repositórios

- **Status:** Aceito
- **Data:** 2026-09-06

## Contexto

O enunciado exige organizar o projeto em quatro repositórios separados, cada um com CI/CD e
deploy automático, e branch `main` protegida com PR obrigatório.

## Decisão

Separar por **ciclo de vida e responsabilidade**:

| Repositório | Conteúdo | Ritmo de mudança |
| --- | --- | --- |
| `oficina-lambda-auth` | Function de auth + API Gateway | Médio |
| `oficina-infra-k8s` | Terraform VPC + EKS + add-ons | Baixo |
| `oficina-infra-database` | Terraform RDS MySQL | Baixo |
| `oficina-app` | App .NET + manifestos K8s | Alto |

Compartilhamento de outputs entre repos via **estado remoto S3** + `terraform_remote_state`
(bucket + trava DynamoDB). Ordem de provisionamento: `infra-k8s` → `infra-database` →
`lambda-auth` → `oficina-app`.

## Consequências

- **Positivas:** blast radius reduzido (mudar o app não toca a infra), pipelines e permissões
  independentes, histórico e ownership claros.
- **Negativas / mitigação:** coordenação de contratos entre repos (outputs) — versionados e
  documentados; a ordem de `apply` é descrita nos READMEs para evitar dependências quebradas.
