# Tech Challenge FIAP — Oficina Mecânica Backend

API REST para gestão de uma oficina mecânica de médio porte, desenvolvida como Tech Challenge do curso de Software Architecture (15SOAT) da FIAP.

---

## Sumário

- [Dicionário de Linguagem Ubíqua](#dicionário-de-linguagem-ubíqua)
- [Stack Tecnológica](#stack-tecnológica)
- [Arquitetura](#arquitetura)
- [Pré-requisitos](#pré-requisitos)
- [Como Executar](#como-executar)
- [Configuração](#configuração)
- [Endpoints da API](#endpoints-da-api)
- [Fluxo de Status da OS](#fluxo-de-status-da-os)
- [Autenticação](#autenticação)
- [Testes](#testes)
- [Análise de Segurança](#análise-de-segurança)
- [Estrutura do Projeto](#estrutura-do-projeto)

---

## Dicionário de Linguagem Ubíqua

| Termo                     | Definição                                                                                                        |
| ------------------------- | ---------------------------------------------------------------------------------------------------------------- |
| **Ordem de Serviço (OS)** | Entidade principal que agrega o cliente, o veículo, os serviços e as peças de um atendimento.                    |
| **Cliente**               | Pessoa física ou jurídica proprietária do veículo, identificada por CPF ou CNPJ.                                 |
| **Veículo**               | Automóvel pertencente a um cliente, identificado pela placa (formato antigo ou Mercosul).                        |
| **Serviço**               | Mão de obra executada na OS (ex.: alinhamento, troca de óleo). Possui preço-base cadastrado.                     |
| **Insumo / Peça**         | Material físico utilizado no reparo. Possui controle de estoque mínimo e valor comercial.                        |
| **Orçamento**             | Somatório automático de serviços e peças da OS, enviado para aprovação do cliente.                               |
| **Status da OS**          | Estado do ciclo de vida: Recebida → Em Diagnóstico → Aguardando Aprovação → Em Execução → Finalizada → Entregue. |
| **Tempo de Execução**     | Métrica que monitora a duração do serviço para análise de eficiência.                                            |
| **CRUD Administrativo**   | Operações de gestão dos cadastros-base (clientes, veículos, serviços, peças) protegidas por JWT.                 |

---

# Domain Storytelling

![Fluxo - Gestão de peças e insumos](docs/gestao-pecas-e-insumos.png)

![Fluxo - Criação e acompanhamento de OS](docs/criacao-e-acompanhamento-de-os.png)

# Event Storming

## Stack Tecnológica

| Camada         | Tecnologia                                                 |
| -------------- | ---------------------------------------------------------- |
| Runtime        | .NET 8.0 / ASP.NET Core                                    |
| ORM            | Entity Framework Core 8 + Pomelo MySQL                     |
| Banco de dados | MySQL 8.4                                                  |
| Autenticação   | JWT Bearer (Microsoft.AspNetCore.Authentication.JwtBearer) |
| Validação      | FluentValidation 11.3                                      |
| Logging        | Serilog (console + arquivo rotativo)                       |
| Documentação   | Swagger / Swashbuckle 6.9                                  |
| Testes         | xUnit + WebApplicationFactory + SQLite in-memory           |
| Containers     | Docker + Docker Compose                                    |

---

## Arquitetura

Monolito em camadas simples (single-project), organizado em:

```
Controllers  ──▶  Services  ──▶  AppDbContext (EF Core)  ──▶  MySQL
                     │
                 Validators (FluentValidation)
                 DTOs (records imutáveis)
                 Models (entidades de domínio)
```

**Padrão de resposta de serviço:** `ServiceResult<T>` encapsula sucesso/erro e o HTTP status code correspondente, mantendo os controllers finos.

---

## Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (para rodar com Docker Compose)
- MySQL 8.4 (somente se rodar sem Docker)

---

## Como Executar

### Com Docker Compose (recomendado)

```bash
docker-compose up --build
```

A API ficará disponível em `http://localhost:8080`.  
O Swagger UI estará em `http://localhost:8080/swagger`.

### Localmente (sem Docker)

1. Garanta que um MySQL 8.4 está rodando e ajuste a connection string em `appsettings.json`.
2. Execute:

```bash
cd OficinaMecanicaBackend/src/OficinaMecanicaBackend
dotnet run
```

As migrations são aplicadas automaticamente na inicialização.

---

## Configuração

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;port=3306;database=OficinaMecanica;user=root;password=SuaSenha"
  },
  "Jwt": {
    "Key": "OficinaMecanicaSecretKeyMustBe32CharactersMin!",
    "Issuer": "OficinaMecanicaBackend",
    "Audience": "OficinaMecanicaBackend",
    "ExpiresInMinutes": 60,
    "AdminUsername": "admin",
    "AdminPassword": "Admin@123"
  }
}
```

### Variáveis de ambiente (produção / Docker)

Sobrescreva qualquer configuração via variáveis de ambiente usando `__` como separador de seção:

| Variável                               | Exemplo                                                          |
| -------------------------------------- | ---------------------------------------------------------------- |
| `ConnectionStrings__DefaultConnection` | `server=db;database=OficinaMecanica;user=root;password=SuaSenha` |
| `Jwt__Key`                             | `ChaveSecretaForteComMaisDe32Caracteres!`                        |
| `Jwt__AdminUsername`                   | `admin`                                                          |
| `Jwt__AdminPassword`                   | `SenhaForte@2026`                                                |

---

## Endpoints da API

### Autenticação

| Método | Rota              | Auth | Descrição      |
| ------ | ----------------- | ---- | -------------- |
| POST   | `/api/auth/login` | Não  | Gera token JWT |

**Payload de login:**

```json
{ "username": "admin", "password": "Admin@123" }
```

---

### Clientes

| Método | Rota                 | Auth | Descrição                           |
| ------ | -------------------- | ---- | ----------------------------------- |
| GET    | `/api/clientes`      | Sim  | Lista todos os clientes             |
| GET    | `/api/clientes/{id}` | Sim  | Busca cliente por ID                |
| POST   | `/api/clientes`      | Sim  | Cria cliente (CPF ou CNPJ validado) |
| PUT    | `/api/clientes/{id}` | Sim  | Atualiza dados do cliente           |
| DELETE | `/api/clientes/{id}` | Sim  | Remove cliente (409 se possuir OS)  |

---

### Veículos

| Método | Rota                                | Auth | Descrição                     |
| ------ | ----------------------------------- | ---- | ----------------------------- |
| GET    | `/api/veiculos`                     | Sim  | Lista todos os veículos       |
| GET    | `/api/veiculos/{id}`                | Sim  | Busca veículo por ID          |
| GET    | `/api/veiculos/cliente/{clienteId}` | Sim  | Veículos de um cliente        |
| POST   | `/api/veiculos`                     | Sim  | Cria veículo (placa validada) |
| PUT    | `/api/veiculos/{id}`                | Sim  | Atualiza dados do veículo     |
| DELETE | `/api/veiculos/{id}`                | Sim  | Remove veículo                |

---

### Peças

| Método | Rota                       | Auth | Descrição                      |
| ------ | -------------------------- | ---- | ------------------------------ |
| GET    | `/api/pecas`               | Sim  | Lista todas as peças           |
| GET    | `/api/pecas/{id}`          | Sim  | Busca peça por ID              |
| GET    | `/api/pecas/estoque-baixo` | Sim  | Peças abaixo do estoque mínimo |
| POST   | `/api/pecas`               | Sim  | Cadastra peça                  |
| PUT    | `/api/pecas/{id}`          | Sim  | Atualiza peça                  |
| DELETE | `/api/pecas/{id}`          | Sim  | Remove peça                    |

---

### Serviços

| Método | Rota                 | Auth | Descrição               |
| ------ | -------------------- | ---- | ----------------------- |
| GET    | `/api/servicos`      | Sim  | Lista todos os serviços |
| GET    | `/api/servicos/{id}` | Sim  | Busca serviço por ID    |
| POST   | `/api/servicos`      | Sim  | Cadastra serviço        |
| PUT    | `/api/servicos/{id}` | Sim  | Atualiza serviço        |
| DELETE | `/api/servicos/{id}` | Sim  | Remove serviço          |

---

### Ordens de Serviço

| Método | Rota                                         | Auth    | Descrição                                 |
| ------ | -------------------------------------------- | ------- | ----------------------------------------- |
| GET    | `/api/ordens-servico`                        | Sim     | Lista todas as OS                         |
| GET    | `/api/ordens-servico/{id}`                   | Sim     | Busca OS por ID                           |
| POST   | `/api/ordens-servico`                        | Sim     | Cria OS (NumeroOS gerado automaticamente) |
| PUT    | `/api/ordens-servico/{id}/status`            | Sim     | Avança status da OS                       |
| PUT    | `/api/ordens-servico/{id}/aprovar-orcamento` | **Não** | Cliente aprova/recusa orçamento           |
| GET    | `/api/ordens-servico/{id}/status`            | **Não** | Status público da OS para o cliente       |
| POST   | `/api/ordens-servico/{id}/itens`             | Sim     | Adiciona serviço ou peça à OS             |
| DELETE | `/api/ordens-servico/{id}/itens/{itemId}`    | Sim     | Remove item da OS                         |

> Os endpoints de aprovação de orçamento e consulta de status são públicos (sem JWT) para que o cliente possa acompanhar e aprovar remotamente.

---

## Fluxo de Status da OS

```
Recebida
   │
   ▼
Em Diagnóstico
   │
   ▼
Aguardando Aprovação ──── cliente aprova (PUT /aprovar-orcamento)
   │
   ▼ (somente se OrcamentoAprovado = true)
Em Execução
   │
   ▼
Finalizada
   │
   ▼
Entregue
```

Transições fora dessa sequência retornam **400 Bad Request**.  
Tentar avançar para "Em Execução" sem aprovação de orçamento também retorna **400**.

---

## Autenticação

A API usa JWT Bearer. Para autenticar:

1. Faça `POST /api/auth/login` com as credenciais administrativas.
2. Copie o campo `token` da resposta.
3. No Swagger UI, clique em **Authorize** e cole apenas o token (sem o prefixo `Bearer `).
4. Em outros clientes (Postman, curl), envie o header: `Authorization: Bearer <token>`.

O token expira em **60 minutos** por padrão.

---

## Validações de Negócio

| Regra                                                       | Comportamento      |
| ----------------------------------------------------------- | ------------------ |
| CPF validado com dígitos verificadores                      | 400 se inválido    |
| CNPJ alfanumérico (novo formato vigente desde jan/2026)     | 400 se inválido    |
| Placa no formato antigo (`ABC1234`) ou Mercosul (`ABC1D23`) | 400 se inválida    |
| CPF/CNPJ duplicado                                          | 409 Conflict       |
| Placa duplicada                                             | 409 Conflict       |
| Excluir cliente com OS                                      | 409 Conflict       |
| Adicionar peça sem estoque suficiente                       | 400 Bad Request    |
| Estoque de peça abaixo do mínimo                            | LogWarning emitido |
| Transição de status inválida                                | 400 Bad Request    |
| Aprovar orçamento fora do status correto                    | 400 Bad Request    |

---

## Testes

### Executar

```bash
cd OficinaMecanicaBackend
dotnet test
```

### Estrutura

| Categoria              | Localização              | Descrição                                             |
| ---------------------- | ------------------------ | ----------------------------------------------------- |
| Unitários — Validators | `tests/.../Validators/`  | CpfCnpjValidatorTests, PlacaValidatorTests            |
| Unitários — Services   | `tests/.../Services/`    | OrdemServicoServiceTests, PecaServiceTests            |
| Integração             | `tests/.../Integration/` | ClientesControllerTests, OrdensServicoControllerTests |

Os testes de integração usam `WebApplicationFactory<Program>` com SQLite in-memory, substituindo o MySQL. O JWT também é reconfigurado com uma chave de teste para os testes de integração.

### Cobertura

```bash
dotnet test --collect:"XPlat Code Coverage"
```

O relatório é gerado em `tests/OficinaMecanicaBackend.Tests/TestResults/*/coverage.cobertura.xml`.

---

## Análise de Segurança

O projeto inclui configuração para dois tipos de varredura de segurança: **SAST** (análise estática do código) via SonarQube e **DAST** (análise dinâmica da API em execução) via OWASP ZAP.

Os relatórios gerados estão em [`security/reports/`](security/reports/).

### Pré-requisitos adicionais

- Docker Desktop em execução
- `dotnet-sonarscanner` (instalado automaticamente pelo script)
- Token SonarQube (gerado após subir o container)

---

### SonarQube — Análise Estática (SAST)

Analisa o código-fonte em busca de vulnerabilidades, security hotspots e code smells.

**1. Subir o SonarQube:**

```bash
docker compose -f security/docker-compose.security.yml up sonarqube
```

Aguarde o container ficar saudável (~2 min) e acesse `http://localhost:9000`.  
Login padrão: `admin` / `admin` (será solicitada troca de senha no primeiro acesso).

**2. Gerar um token de acesso:**

`http://localhost:9000` → My Account → Security → Generate Token

**3. Executar o scan:**

```powershell
powershell -ExecutionPolicy Bypass -File .\security\run-sonar-scan.ps1 -SonarToken "sqp_seu_token_aqui"
```

O script compila o projeto, roda os testes com cobertura e envia os resultados automaticamente.  
Ao final, o dashboard estará disponível em:  
`http://localhost:9000/dashboard?id=oficina-mecanica-backend`

> Relatório detalhado: [`security/reports/sonarqube-report.md`](security/reports/sonarqube-report.md)

---

### OWASP ZAP — Varredura Dinâmica (DAST)

Testa a API em execução em busca de vulnerabilidades de runtime (força bruta, headers ausentes, endpoints expostos, etc.).

**1. Garantir que a API está rodando:**

```bash
cd OficinaMecanicaBackend
docker compose up
```

**2. Executar o scan ZAP:**

```powershell
powershell -ExecutionPolicy Bypass -File .\security\run-zap-scan.ps1
```

Os relatórios HTML, JSON e XML são salvos em `security/reports/`:

| Arquivo                    | Formato              |
| -------------------------- | -------------------- |
| `zap-baseline-report.html` | Legível no navegador |
| `zap-baseline-report.json` | Análise programática |
| `zap-baseline-report.xml`  | Integração CI/CD     |

Para um scan mais profundo (com spider autenticado e active scan completo):

```powershell
powershell -ExecutionPolicy Bypass -File .\security\run-zap-scan.ps1 -ScanType Full
```

> Relatório detalhado: [`security/reports/zap-report.md`](security/reports/zap-report.md)

---

### Resumo das Vulnerabilidades Identificadas

| Ferramenta | Severidade     | Total | Principais Achados                                                            |
| ---------- | -------------- | ----- | ----------------------------------------------------------------------------- |
| SonarQube  | Critical       | 2     | Credenciais e chave JWT hardcoded em `appsettings.json`                       |
| SonarQube  | Hotspot High   | 2     | Senha comparada sem hash; CORS permissivo                                     |
| SonarQube  | Hotspot Medium | 3     | Senha MySQL no docker-compose; porta 3306 exposta; security headers ausentes  |
| ZAP        | Alto           | 1     | Sem rate limiting no endpoint de login                                        |
| ZAP        | Médio          | 4     | CSP ausente; anti-clickjacking; aprovação anônima sem validação; HSTS ausente |

---

## Estrutura do Projeto

```
OficinaMecanicaBackend/
├── src/OficinaMecanicaBackend/
│   ├── Controllers/          # AuthController, ClientesController, VeiculosController,
│   │                         #   PecasController, ServicosController, OrdensServicoController
│   ├── Data/
│   │   └── AppDbContext.cs   # EF Core DbContext com Fluent API
│   ├── DTOs/                 # Records imutáveis por domínio (Auth, Clientes, Veiculos, ...)
│   ├── Migrations/           # Migrações EF Core
│   ├── Models/
│   │   ├── Enums/            # StatusOrdemServico, TipoItemOrdemServico
│   │   └── *.cs              # Cliente, Veiculo, Peca, Servico, OrdemServico, ItemOrdemServico
│   ├── Services/
│   │   ├── ServiceResult.cs  # Wrapper de resultado tipado com HTTP status code
│   │   └── *.cs              # AuthService, ClienteService, VeiculoService, ...
│   ├── Validators/           # FluentValidation + CpfCnpjValidator + PlacaValidator
│   ├── appsettings.json
│   ├── Dockerfile
│   └── Program.cs
├── tests/OficinaMecanicaBackend.Tests/
│   ├── Infrastructure/       # CustomWebApplicationFactory, IntegrationTestCollection
│   ├── Integration/          # Testes de integração HTTP
│   ├── Services/             # Testes unitários de services
│   └── Validators/           # Testes unitários de validators
└── docker-compose.yml
```

---

## Banco de Dados

O schema é gerenciado via EF Core Migrations e aplicado automaticamente na inicialização.

| Tabela              | Descrição                                                    |
| ------------------- | ------------------------------------------------------------ |
| `Clientes`          | Cadastro de clientes (CPF/CNPJ único)                        |
| `Veiculos`          | Veículos vinculados a clientes (placa única)                 |
| `Pecas`             | Catálogo de peças com controle de estoque                    |
| `Servicos`          | Catálogo de serviços com preço-base                          |
| `OrdensServico`     | Ordens de serviço (NumeroOS único, formato `OS-YYYY-000001`) |
| `ItensOrdenServico` | Itens de OS (serviços e peças com preço snapshot)            |
