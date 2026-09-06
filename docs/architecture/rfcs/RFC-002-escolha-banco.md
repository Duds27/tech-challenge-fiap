# RFC-002 — Escolha do banco de dados: Amazon RDS for MySQL

- **Status:** Aceita
- **Data:** 2026-09-06
- **Autores:** Equipe 15SOAT

## Contexto

O domínio (clientes, veículos, ordens de serviço, itens, peças, serviços) é relacional e
transacional. A aplicação das Fases 1–2 já persiste em MySQL via EF Core. A Fase 3 exige um
**banco gerenciado**, com consistência, performance e alta disponibilidade, e uma justificativa
formal para a escolha.

## Opções consideradas

- **Manter MySQL (RDS for MySQL).**
- **Migrar para PostgreSQL (RDS for PostgreSQL).**
- **Usar um NoSQL (DynamoDB).** — descartado: o modelo é normalizado, com integridade
  referencial e transações multi-tabela; NoSQL exigiria desnormalização e perda de garantias.

## Decisão

Adotar **Amazon RDS for MySQL (Multi-AZ)**, mantendo o motor atual.

## Consequências

- **Positivas:** mínima mudança na aplicação (provider EF Core inalterado), reuso de migrations
  e dos testes de integração (Testcontainers/MySQL); HA via Multi-AZ, backups e patching gerenciados.
- **Negativas / mitigação:** dependência do RDS (custo) — instância pequena para o desafio; para
  escalar leitura, adicionar réplicas de leitura no futuro.
- **Modelagem:** ver [database-model.md](../database-model.md) — inclui o novo `Cliente.Ativo`
  (status para autenticação) e a revisão de índices únicos.
