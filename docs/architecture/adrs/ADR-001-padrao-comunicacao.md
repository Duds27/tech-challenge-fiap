# ADR-001 — Padrão de comunicação: API Gateway + VPC Link

- **Status:** Aceito
- **Data:** 2026-09-06

## Contexto

O API Gateway é o ponto único de entrada. As rotas `/api/*` precisam alcançar a API .NET
que roda no EKS em subnets privadas, sem expor o cluster diretamente à internet.

## Decisão

- **API Gateway (HTTP API)** como fachada única (roteamento + autorização).
- Integração da rota pública `POST /auth` via **AWS_PROXY** (Lambda).
- Integração das rotas `/api/*` via **HTTP_PROXY + VPC Link** para um **NLB interno** que
  fronteia o Service da API no EKS. O tráfego permanece dentro da VPC.
- Autorização das rotas protegidas por **Lambda Authorizer** (CUSTOM).

## Consequências

- **Positivas:** cluster não exposto publicamente; TLS/limites/observabilidade centralizados no gateway;
  separação clara entre borda (gateway) e execução (EKS).
- **Negativas / mitigação:** salto extra (gateway → VPC Link → NLB) adiciona latência pequena;
  o NLB interno e o VPC Link são provisionados pelo repo `infra-k8s` e consumidos por `lambda-auth`
  via `terraform_remote_state`.
