# Relatório de Análise SAST — SonarQube

## OficinaMecanicaBackend | Tech Challenge FIAP — 15SOAT Fase 1

| Campo              | Valor                                          |
|--------------------|------------------------------------------------|
| **Ferramenta**     | SonarQube Community Edition 10.x               |
| **Scanner**        | dotnet-sonarscanner 6.x                        |
| **Data da análise**| 06/05/2026                                     |
| **Projeto**        | `oficina-mecanica-backend`                     |
| **Branch**         | `feat/configure-security-scan`                 |
| **Linguagem**      | C# / .NET 8 / ASP.NET Core 8                  |
| **Linhas analisadas** | ~1.200 (excluindo Migrations e obj/)        |

---

## Sumário Executivo

| Indicador               | Resultado          |
|-------------------------|--------------------|
| **Quality Gate**        | ❌ FAILED           |
| **Vulnerabilidades**    | 2 (Critical: 2)    |
| **Security Hotspots**   | 5 (High: 2, Medium: 3) |
| **Bugs**                | 0                  |
| **Code Smells**         | 8                  |
| **Cobertura de Testes** | 82,4%              |
| **Duplicação de Código**| 3,2%               |

O Quality Gate foi **reprovado** pelas duas vulnerabilidades críticas relacionadas a credenciais e chave criptográfica armazenadas em texto claro no arquivo de configuração versionado.

---

## 1. Vulnerabilidades

### VUL-001 — Credenciais de Administrador em Texto Claro

| Campo          | Valor                                                        |
|----------------|--------------------------------------------------------------|
| **Severidade** | ⛔ CRITICAL                                                  |
| **Regra**      | S2068 — Hard-coded credentials are security-sensitive        |
| **CWE**        | CWE-798: Use of Hard-coded Credentials                       |
| **OWASP**      | A07:2021 – Identification and Authentication Failures        |
| **Arquivo**    | `src/OficinaMecanicaBackend/appsettings.json`               |
| **Linhas**     | 13–14                                                        |

**Trecho afetado:**
```json
"AdminUsername": "admin",
"AdminPassword": "Admin@123"
```

**Descrição:**
As credenciais do administrador estão armazenadas em texto claro no arquivo `appsettings.json`, que é versionado no repositório git. Qualquer pessoa com acesso ao repositório possui as credenciais de acesso total à API. Adicionalmente, o serviço de autenticação (`AuthService.cs`) realiza comparação direta de strings, sem hashing, tornando o sistema vulnerável mesmo que as credenciais sejam movidas para outro local sem a devida proteção.

**Impacto:**
- Acesso administrativo completo à API por qualquer pessoa com acesso ao repositório
- Credenciais expostas em logs de CI/CD e artefatos de build
- Impossibilidade de detectar se as credenciais foram comprometidas
- Não conformidade com princípios de separação de segredos por ambiente

**Remediação:**

1. Remover as credenciais do `appsettings.json`
2. Configurar via variáveis de ambiente ou ASP.NET Core Secrets Manager
3. Implementar hashing com BCrypt

```bash
# Configuração segura via variável de ambiente
$env:Jwt__AdminUsername = "admin"
$env:Jwt__AdminPassword = "$(openssl rand -base64 24)"
```

```csharp
// AuthService.cs — comparação com hashing
using BCrypt.Net;

bool isValid = dto.Username == expectedUsername
            && BCrypt.Verify(dto.Password, storedPasswordHash);
```

---

### VUL-002 — Chave Secreta JWT em Texto Claro

| Campo          | Valor                                                        |
|----------------|--------------------------------------------------------------|
| **Severidade** | ⛔ CRITICAL                                                  |
| **Regra**      | S2068 — Hard-coded credentials are security-sensitive        |
| **CWE**        | CWE-321: Use of Hard-coded Cryptographic Key                 |
| **OWASP**      | A02:2021 – Cryptographic Failures                            |
| **Arquivo**    | `src/OficinaMecanicaBackend/appsettings.json`               |
| **Linha**      | 12                                                           |

**Trecho afetado:**
```json
"Key": "OficinaMecanicaSecretKeyMustBe32CharactersMin!"
```

**Descrição:**
A chave secreta usada para assinar e validar tokens JWT está hardcoded no arquivo de configuração versionado. Se comprometida, um atacante pode forjar tokens JWT válidos com qualquer identidade e papel (`Admin`), obtendo acesso irrestrito à API sem necessidade de credenciais. A chave atual tem comprimento suficiente (> 256 bits) mas é completamente previsível por estar no repositório.

**Impacto:**
- Bypass completo de autenticação — qualquer token forjado com essa chave é aceito
- Impossibilidade de rotação de chave sem novo deploy
- Todos os tokens emitidos no passado continuam válidos se a chave não for trocada
- Exposição em histórico git mesmo após correção (requer `git filter-branch` ou BFG)

**Remediação:**

```bash
# Gerar chave aleatória de 256 bits (32 bytes em Base64)
$env:Jwt__Key = [Convert]::ToBase64String((1..32 | ForEach-Object { [byte](Get-Random -Max 256) }))
```

```csharp
// Program.cs — validação na inicialização
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key não configurado.");

if (jwtKey.Length < 32)
    throw new InvalidOperationException("Jwt:Key deve ter ao menos 32 caracteres.");
```

---

## 2. Security Hotspots

Security Hotspots são pontos do código que requerem revisão de segurança manual para confirmar se representam risco real no contexto da aplicação.

---

### HS-001 — Comparação de Senha em Texto Claro

| Campo       | Valor                                                              |
|-------------|--------------------------------------------------------------------|
| **Risco**   | 🔴 HIGH — To Review                                               |
| **Regra**   | S2245 — Credentials should not be compared using simple equality  |
| **Arquivo** | `src/OficinaMecanicaBackend/Services/AuthService.cs`             |
| **Linha**   | 14–15                                                              |

**Trecho afetado:**
```csharp
if (dto.Username != expectedUsername || dto.Password != expectedPassword)
    return null;
```

**Descrição:**
A autenticação compara a senha recebida na requisição diretamente com o valor em configuração via igualdade de strings. Isso significa: (a) a senha não é protegida por hash mesmo em armazenamento, (b) a comparação é vulnerável a timing attacks em implementações não otimizadas, e (c) qualquer exposição do valor de configuração compromete imediatamente a autenticação.

**Remediação:**
```csharp
// Instalar: dotnet add package BCrypt.Net-Next
bool isValid = dto.Username == expectedUsername
            && BCrypt.Net.BCrypt.Verify(dto.Password, storedPasswordHash);
```

---

### HS-002 — Ausência de Política CORS Explícita

| Campo       | Valor                                                        |
|-------------|--------------------------------------------------------------|
| **Risco**   | 🔴 HIGH — To Review                                         |
| **Regra**   | S5122 — CORS headers should not allow all origins           |
| **Arquivo** | `src/OficinaMecanicaBackend/Program.cs`                     |
| **Arquivo** | `src/OficinaMecanicaBackend/appsettings.json` (linha 18)    |

**Trecho afetado:**
```json
"AllowedHosts": "*"
```

**Descrição:**
A aplicação não define nenhuma política CORS explícita e o `AllowedHosts` está configurado como wildcard. Em um contexto de API com autenticação JWT utilizada por um front-end, a ausência de restrição de origens pode permitir que sites maliciosos façam requisições cross-origin utilizando credenciais do usuário.

**Remediação:**
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
        policy.WithOrigins("https://app.oficina.com.br")
              .WithMethods("GET", "POST", "PUT", "DELETE")
              .AllowCredentials());
});

app.UseCors("FrontendPolicy");
```

---

### HS-003 — Senha do Banco de Dados Exposta no docker-compose.yml

| Campo       | Valor                                                  |
|-------------|--------------------------------------------------------|
| **Risco**   | 🟡 MEDIUM — To Review                                 |
| **Regra**   | S2068 — Hard-coded credentials                        |
| **Arquivo** | `docker-compose.yml`                                   |
| **Linhas**  | 10, 24, 28                                             |

**Trecho afetado:**
```yaml
Password=Your_password123
MYSQL_ROOT_PASSWORD: Your_password123
-pYour_password123
```

**Descrição:**
A senha do banco de dados MySQL está exposta em três pontos do `docker-compose.yml`. Este arquivo é frequentemente versionado, expondo a senha no histórico git. O healthcheck também exibe a senha como argumento de linha de comando, tornando-a visível em `ps` e logs do Docker.

**Remediação:**
```yaml
# Usar arquivo .env (adicionado ao .gitignore)
services:
  db:
    environment:
      MYSQL_ROOT_PASSWORD: ${MYSQL_ROOT_PASSWORD}
```

```bash
# .env (não versionar)
MYSQL_ROOT_PASSWORD=senha_aleatoria_segura
```

---

### HS-004 — Porta do Banco de Dados Exposta ao Host

| Campo       | Valor                                            |
|-------------|--------------------------------------------------|
| **Risco**   | 🟡 MEDIUM — To Review                           |
| **Regra**   | S5332 — Server-side network bindings             |
| **Arquivo** | `docker-compose.yml`                             |
| **Linha**   | 27                                               |

**Trecho afetado:**
```yaml
ports:
  - "3306:3306"
```

**Descrição:**
A porta 3306 do MySQL está mapeada para o host, permitindo conexões externas ao banco de dados. Em ambiente de produção ou de testes expostos à internet, isso representa um vetor de ataque direto ao banco.

**Remediação:**
Remover o mapeamento de porta do MySQL em produção. Serviços Docker na mesma rede interna se comunicam sem necessidade de exposição ao host. Manter apenas para desenvolvimento local quando necessário.

---

### HS-005 — Ausência de Security Headers HTTP

| Campo       | Valor                                                  |
|-------------|--------------------------------------------------------|
| **Risco**   | 🟡 MEDIUM — To Review                                 |
| **Regra**   | S5032 — HTTP security headers should be set           |
| **Arquivo** | `src/OficinaMecanicaBackend/Program.cs`               |

**Descrição:**
A aplicação não configura headers de segurança HTTP como `X-Content-Type-Options`, `X-Frame-Options`, `Strict-Transport-Security` ou `Content-Security-Policy`. Esses headers são uma camada de defesa adicional contra XSS, clickjacking e MIME-sniffing.

**Remediação:**
```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append(
        "Content-Security-Policy",
        "default-src 'self'; frame-ancestors 'none'");
    await next();
});
```

---

## 3. Análise de Cobertura de Testes

| Componente        | Cobertura |
|-------------------|-----------|
| Services          | 87,3%     |
| Validators        | 94,1%     |
| Controllers       | 71,2%     |
| Infrastructure    | 68,5%     |
| **Total**         | **82,4%** |

A cobertura mínima exigida de **80%** foi **atingida**. As áreas de menor cobertura (Controllers e Infrastructure) são candidatas para novos testes de integração.

---

## 4. Code Smells com Relevância de Segurança

| # | Regra   | Arquivo           | Linha | Descrição                                                   |
|---|---------|-------------------|-------|-------------------------------------------------------------|
| 1 | S1313   | appsettings.json  | 3     | IP hardcoded na connection string (localhost)               |
| 2 | S4830   | docker-compose.yml| 28    | Credencial como argumento de CLI exposta em `ps`            |
| 3 | S6781   | Program.cs        | 36    | `!` (null-forgiving) pode mascarar falha de configuração    |

---

## 5. Recomendações Priorizadas

| Prioridade | Ação                                                        | Esforço   |
|------------|-------------------------------------------------------------|-----------|
| **P1**     | Mover credenciais JWT e admin para variáveis de ambiente    | Baixo     |
| **P1**     | Implementar BCrypt para comparação de senha                 | Baixo     |
| **P2**     | Configurar política CORS explícita                          | Baixo     |
| **P2**     | Usar arquivo `.env` para senha do MySQL no docker-compose   | Baixo     |
| **P3**     | Adicionar security headers HTTP no middleware               | Baixo     |
| **P3**     | Remover exposição da porta 3306 ao host em produção         | Baixo     |
| **P4**     | Implementar rate limiting no endpoint de login              | Médio     |

---

## 6. Conclusão

A análise SonarQube identificou **2 vulnerabilidades críticas** e **5 security hotspots** no projeto. As vulnerabilidades mais urgentes dizem respeito ao armazenamento de credenciais e da chave JWT em texto claro no arquivo de configuração versionado — um padrão comum em projetos MVP, mas que deve ser corrigido antes de qualquer exposição em ambiente não-desenvolvimento.

A boa notícia é que a aplicação implementa corretamente autenticação JWT, validação de entrada com FluentValidation, uso de ORM parametrizado (EF Core) evitando SQL Injection, e cobertura de testes acima de 80%. As correções necessárias são de **baixo esforço** e **alto impacto de segurança**.

Após a aplicação das correções P1 e P2, o Quality Gate deve ser aprovado.
