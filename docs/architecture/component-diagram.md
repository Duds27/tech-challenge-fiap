# Diagrama de Componentes

Visão de nuvem (AWS) com APIs, banco e monitoramento.

```mermaid
flowchart TB
  subgraph internet[Internet]
    cliente[Cliente / Frontend]
    dev[Desenvolvedores]
  end

  subgraph github[GitHub]
    repos["4 repos + GitHub Actions<br/>(CI/CD, OIDC → AWS)"]
  end
  dev -->|push / PR| repos

  subgraph aws[AWS]
    apigw["API Gateway (HTTP API)<br/>rotas + roteamento"]

    subgraph serverless[Serverless]
      lauth["Lambda auth<br/>valida CPF + emite JWT"]
      lauthz["Lambda Authorizer<br/>valida JWT HS256"]
    end

    subgraph vpc[VPC]
      subgraph eks["EKS (namespace oficina)"]
        nlb[NLB interno]
        api["Deployment API .NET<br/>2..N réplicas"]
        hpa["HPA (CPU/Mem)"]
        nlb --> api
        hpa -. escala .-> api
      end
      rds[("RDS MySQL<br/>Multi-AZ")]
    end

    secrets[("Secrets Manager<br/>chave JWT + creds DB")]
    ecr[("ECR<br/>imagem da API")]

    apigw -->|POST /auth| lauth
    apigw -->|/api/* CUSTOM authorizer| lauthz
    apigw -->|/api/* via VPC Link| nlb
    api -->|EF Core| rds
    lauth --> rds
    lauth -.-> secrets
    lauthz -.-> secrets
    api -.-> secrets
    api -. pull imagem .- ecr
  end

  cliente -->|HTTPS| apigw
  repos -->|deploy| aws

  subgraph obs[Observabilidade]
    nr["New Relic<br/>APM + Infra + Logs + Alertas"]
  end
  api -->|traces, métricas, logs JSON| nr
  eks -->|nri-bundle: CPU/mem/uptime| nr
  lauth -->|logs| nr
```

## Componentes

| Componente | Serviço AWS | Responsabilidade |
| --- | --- | --- |
| API Gateway | API Gateway (HTTP API) | Ponto único de entrada, roteamento e proteção das rotas `/api/*`. |
| Lambda auth | AWS Lambda (Node 20) | Valida CPF, consulta cliente no RDS, emite JWT. |
| Lambda Authorizer | AWS Lambda (Node 20) | Valida o JWT HS256 antes de encaminhar ao app. |
| API .NET | EKS (Deployment + Service/NLB) | Regras de negócio (OS, clientes, veículos, peças, serviços). |
| Escalabilidade | HPA + Cluster Autoscaler | Ajuste de réplicas/nós por CPU/memória. |
| Banco | RDS for MySQL (Multi-AZ) | Persistência transacional com alta disponibilidade. |
| Segredos | Secrets Manager | Chave do JWT e credenciais do banco (fonte única). |
| Registry | ECR | Imagens Docker da API. |
| Observabilidade | New Relic | APM, métricas de infra, logs estruturados, dashboards e alertas. |
| CI/CD | GitHub Actions | Build, testes e deploy automático (homolog/prod) por repositório. |
