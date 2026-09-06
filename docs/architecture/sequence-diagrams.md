# Diagramas de Sequência

## 1. Autenticação por CPF

```mermaid
sequenceDiagram
    autonumber
    participant C as Cliente
    participant GW as API Gateway
    participant L as Lambda auth
    participant SM as Secrets Manager
    participant DB as RDS MySQL

    C->>GW: POST /auth { cpf }
    GW->>L: invoca (AWS_PROXY)
    L->>L: valida CPF (formato + dígitos verificadores)
    alt CPF inválido
        L-->>C: 400 { error: "CPF inválido." }
    else CPF válido
        L->>SM: GetSecretValue (chave JWT + creds DB) [cache]
        L->>DB: SELECT Id, Nome, CpfCnpj, Ativo WHERE CpfCnpj = ?
        alt cliente não encontrado
            L-->>C: 401 { error: "Cliente não encontrado." }
        else cliente inativo
            L-->>C: 403 { error: "Cliente inativo." }
        else cliente ativo
            L->>L: emite JWT HS256 (sub, cpf, role=Cliente, exp)
            L-->>C: 200 { token, expiresAt }
        end
    end
```

## 2. Abertura de Ordem de Serviço (rota protegida)

```mermaid
sequenceDiagram
    autonumber
    participant C as Cliente
    participant GW as API Gateway
    participant AZ as Lambda Authorizer
    participant API as API .NET (EKS)
    participant DB as RDS MySQL
    participant NR as New Relic

    C->>GW: POST /api/ordens-servico (Bearer JWT)
    GW->>AZ: autoriza (valida JWT HS256)
    alt token inválido/expirado
        AZ-->>GW: isAuthorized=false
        GW-->>C: 401 Unauthorized
    else token válido
        AZ-->>GW: isAuthorized=true (context: clienteId, cpf)
        GW->>API: encaminha via VPC Link → NLB interno
        API->>API: valida payload + regras de domínio (status inicial = Recebida)
        API->>DB: INSERT OrdemServico
        API->>NR: métrica "os_criada" + log JSON (CorrelationId)
        API-->>C: 201 Created { id, numeroOS, status }
    end
```

> O app .NET mantém validação JWT Bearer própria (defesa em profundidade), usando a
> mesma chave/issuer/audience via Secrets Manager.
