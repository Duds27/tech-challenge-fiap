# RFC-003 — Estratégia de autenticação: CPF + Lambda + JWT

- **Status:** Aceita
- **Data:** 2026-09-06
- **Autores:** Equipe 15SOAT

## Contexto

As rotas sensíveis devem ser protegidas com **autenticação via CPF**. A emissão de token deve
ficar em uma **Function Serverless** que valida o CPF, consulta o cliente na base e devolve um
JWT para consumo das APIs protegidas. O app .NET já valida JWT Bearer (HS256) com issuer/audience.

## Opções consideradas

1. **Cognito (User Pools).** Poderoso, mas o identificador aqui é CPF (não e-mail/senha) e não há
   cadastro/senha do cliente — adaptá-lo seria custoso e fugiria do enunciado.
2. **JWT Authorizer nativo do API Gateway.** Exige emissor OIDC/JWKS (assimétrico). Nossa emissão
   é HS256 (simétrica), compartilhada com o app .NET.
3. **Lambda (Node) emitindo JWT HS256 + Lambda Authorizer validando.** ✅

## Decisão

- `POST /auth` → **Lambda auth** (Node/TS): valida CPF (mesma regra do app), consulta `Cliente`
  por `CpfCnpj`, verifica `Ativo`, emite **JWT HS256** (claims `sub`, `cpf`, `role=Cliente`, `exp`).
- Rotas `/api/*` → **Lambda Authorizer** (SIMPLE) valida o JWT antes de encaminhar ao EKS.
- **Chave única** do JWT no **Secrets Manager**, consumida por Lambda, Authorizer e app .NET.
- O app .NET mantém validação Bearer própria (defesa em profundidade).

## Consequências

- **Positivas:** autenticação desacoplada e serverless (escala a zero), consistência de regra de
  CPF, tokens interoperáveis entre gateway e app.
- **Negativas / mitigação:** cold start da Lambda na VPC — pool `mysql2` + cache de segredos entre
  invocações. HS256 exige guardar bem o segredo (Secrets Manager + IAM restrito). Evolução futura
  possível: RS256 + JWKS para usar o JWT Authorizer nativo.
