# RFC-001 — Escolha da nuvem: AWS

- **Status:** Aceita
- **Data:** 2026-09-06
- **Autores:** Equipe 15SOAT

## Contexto

A Fase 3 exige provisionar, via IaC, um API Gateway, uma Function Serverless, um banco
gerenciado e um cluster Kubernetes escalável, com deploy automático. É preciso escolher
um provedor que ofereça todos esses blocos de forma integrada e com bom material de apoio.

## Opções consideradas

| Critério | AWS | GCP | Azure |
| --- | --- | --- | --- |
| API Gateway | API Gateway (HTTP/REST) | API Gateway / Apigee | API Management |
| Serverless | Lambda | Cloud Functions | Azure Functions |
| K8s gerenciado | EKS | GKE | AKS |
| Banco gerenciado | RDS MySQL | Cloud SQL | Azure DB for MySQL |
| Terraform (maturidade) | Muito alta | Alta | Alta |
| Alinhamento ao enunciado | Exemplos citados no PDF | — | — |

## Decisão

Adotar **AWS**: API Gateway + Lambda + EKS + RDS MySQL, tudo provisionado com Terraform.
Motivos: (1) cobre todos os requisitos com serviços de primeira classe; (2) provider
Terraform maduro e amplamente documentado; (3) alinhamento com os exemplos do enunciado;
(4) integração simples com New Relic (observabilidade).

## Consequências

- **Positivas:** ecossistema coeso (IAM, Secrets Manager, ECR, VPC Link), farta referência,
  OIDC nativo para GitHub Actions.
- **Negativas / mitigação:** custo de EKS + NAT + RDS Multi-AZ — usar instâncias pequenas e
  `terraform destroy` após a avaliação; curva de rede (VPC/subnets/SG) documentada nos repos de infra.
