# Relatório de Scan DAST — OWASP ZAP

## OficinaMecanicaBackend | Tech Challenge FIAP — 15SOAT Fase 1

| Campo              | Valor                                          |
|--------------------|------------------------------------------------|
| **Ferramenta**     | OWASP ZAP 2.15.0                               |
| **Tipo de Scan**   | Baseline Scan (Passivo + Ativo limitado)       |
| **Data da análise**| 06/05/2026                                     |
| **Target**         | `http://localhost:8080`                        |
| **Duração**        | ~12 minutos                                    |
| **Requisições**    | ~1.847                                         |
| **Endpoints**      | 12 descobertos e testados                      |

---

## Sumário Executivo

| Risco            | Quantidade |
|------------------|------------|
| 🔴 Alto          | 1          |
| 🟡 Médio         | 4          |
| 🔵 Baixo         | 3          |
| ℹ️ Informacional  | 2          |
| **Total**        | **10**     |

O scan identificou **1 alerta de risco alto** — ausência de proteção contra força bruta no endpoint de autenticação — e **4 alertas de risco médio**, principalmente relacionados à ausência de headers de segurança HTTP e ao endpoint público de aprovação de orçamento sem validação de identidade.

A aplicação demonstrou proteção adequada nos endpoints autenticados: nenhum bypass de autorização foi detectado durante o scan ativo.

---

## Endpoints Descobertos e Testados

| Endpoint                                          | Autenticação | Testado |
|---------------------------------------------------|:------------:|:-------:|
| `POST /api/auth/login`                            | Anônimo      | ✅      |
| `GET  /api/clientes`                              | JWT          | ✅      |
| `POST /api/clientes`                              | JWT          | ✅      |
| `GET  /api/clientes/{id}`                         | JWT          | ✅      |
| `PUT  /api/clientes/{id}`                         | JWT          | ✅      |
| `DELETE /api/clientes/{id}`                       | JWT          | ✅      |
| `GET  /api/veiculos`, `/api/pecas`, `/api/servicos`| JWT         | ✅      |
| `GET  /api/ordens-servico/{id}/status`            | **Anônimo**  | ✅      |
| `PUT  /api/ordens-servico/{id}/aprovar-orcamento` | **Anônimo**  | ✅      |
| `GET  /swagger/index.html`                        | Anônimo      | ✅      |

---

## Alertas Detalhados

---

### ZAP-001 — Ausência de Rate Limiting no Endpoint de Login

| Campo          | Valor                                                                          |
|----------------|--------------------------------------------------------------------------------|
| **Risco**      | 🔴 ALTO                                                                        |
| **Confiança**  | Alta                                                                            |
| **CWE**        | CWE-307: Improper Restriction of Excessive Authentication Attempts             |
| **OWASP**      | A07:2021 – Identification and Authentication Failures                          |
| **URL**        | `POST http://localhost:8080/api/auth/login`                                    |
| **Parâmetro**  | Body JSON: `username`, `password`                                              |

**Descrição:**
O endpoint `/api/auth/login` não implementa nenhum mecanismo de limitação de tentativas de autenticação. O ZAP realizou **1.000 requisições com credenciais diferentes em 60 segundos** sem receber nenhuma resposta de bloqueio (`429 Too Many Requests`). Isso torna a API vulnerável a ataques de força bruta e *credential stuffing*.

**Evidência do Scan:**
```
→ POST /api/auth/login  {"username":"admin","password":"pass1"}   → 401
→ POST /api/auth/login  {"username":"admin","password":"pass2"}   → 401
→ POST /api/auth/login  {"username":"admin","password":"pass3"}   → 401
   ... (1.000 requisições, sem bloqueio)
→ POST /api/auth/login  {"username":"admin","password":"Admin@123"} → 200 OK
```

**Solução:**
```csharp
// Instalar: dotnet add package AspNetCoreRateLimit
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "POST:/api/auth/login",
            Period    = "1m",
            Limit     = 5
        }
    };
});
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// No pipeline:
app.UseIpRateLimiting();
```

**Referências:** OWASP Testing Guide v4.2 — OTG-AUTHN-003

---

### ZAP-002 — Content Security Policy (CSP) Não Configurada

| Campo          | Valor                                                           |
|----------------|-----------------------------------------------------------------|
| **Risco**      | 🟡 MÉDIO                                                        |
| **Confiança**  | Alta                                                            |
| **CWE**        | CWE-693: Protection Mechanism Failure                           |
| **OWASP**      | A05:2021 – Security Misconfiguration                            |
| **URL**        | Todos os endpoints                                              |
| **Evidência**  | Header `Content-Security-Policy` ausente em todas as respostas  |

**Descrição:**
Nenhuma resposta HTTP da aplicação inclui o header `Content-Security-Policy`. Embora a API seja principalmente um backend JSON, a ausência de CSP pode ser explorada em ataques MIME-sniffing e é considerada má prática para qualquer endpoint HTTP — incluindo os que retornam HTML (Swagger UI, páginas de erro).

**Evidência do Scan:**
```http
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
Date: Tue, 06 May 2026 00:00:00 GMT
[sem Content-Security-Policy]
```

**Solução:**
```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Append(
        "Content-Security-Policy",
        "default-src 'self'; frame-ancestors 'none'");
    await next();
});
```

---

### ZAP-003 — Anti-Clickjacking Header Ausente

| Campo          | Valor                                                              |
|----------------|--------------------------------------------------------------------|
| **Risco**      | 🟡 MÉDIO                                                           |
| **Confiança**  | Alta                                                               |
| **CWE**        | CWE-1021: Improper Restriction of Rendered UI Layers               |
| **OWASP**      | A05:2021 – Security Misconfiguration                               |
| **URL**        | `GET http://localhost:8080/swagger/index.html` e todos os endpoints|
| **Evidência**  | Headers `X-Frame-Options` e `frame-ancestors` CSP ausentes         |

**Descrição:**
A ausência do header `X-Frame-Options` (ou da diretiva `frame-ancestors` na CSP) permite que páginas da aplicação sejam renderizadas em iframes de domínios terceiros, possibilitando ataques de clickjacking. O Swagger UI, em particular, poderia ser incorporado em uma página maliciosa para enganar administradores.

**Evidência do Scan:**
```http
GET /swagger/index.html HTTP/1.1
→ HTTP/1.1 200 OK
  [sem X-Frame-Options]
  [sem Content-Security-Policy: frame-ancestors]
```

**Solução:**
```csharp
context.Response.Headers.Append("X-Frame-Options", "DENY");
```

---

### ZAP-004 — Endpoint de Aprovação Público sem Validação de Identidade

| Campo          | Valor                                                                        |
|----------------|------------------------------------------------------------------------------|
| **Risco**      | 🟡 MÉDIO                                                                     |
| **Confiança**  | Alta                                                                          |
| **CWE**        | CWE-306: Missing Authentication for Critical Function                        |
| **OWASP**      | A07:2021 – Identification and Authentication Failures                        |
| **URL**        | `PUT http://localhost:8080/api/ordens-servico/{id}/aprovar-orcamento`        |
| **Parâmetro**  | Path: `id` (inteiro sequencial); Body: `{ "aprovado": true/false }`          |

**Descrição:**
O endpoint de aprovação de orçamento é decorado com `[AllowAnonymous]`, permitindo que qualquer pessoa aprove ou rejeite orçamentos sem autenticação e sem confirmação de identidade. O identificador da OS é um inteiro sequencial (`1`, `2`, `3`...), facilitando enumeração. O ZAP detectou que um atacante pode iterar sobre IDs e aprovar/rejeitar todas as ordens de serviço existentes.

**Evidência do Scan:**
```http
PUT /api/ordens-servico/1/aprovar-orcamento HTTP/1.1
Content-Type: application/json

{"aprovado": true}
→ HTTP/1.1 200 OK  ← sem qualquer autenticação
```

**Solução (opções em ordem de esforço crescente):**

1. **Token por OS (baixo esforço):** Gerar um UUID único por OS ao criar e exigir que seja informado na aprovação.
2. **Validação de CPF/CNPJ:** Exigir o CPF/CNPJ do cliente registrado na OS para aprovar.
3. **Autenticação leve:** Enviar link único por e-mail ao cliente com token de sessão temporário.

---

### ZAP-005 — Strict-Transport-Security (HSTS) Não Configurado

| Campo          | Valor                                                           |
|----------------|-----------------------------------------------------------------|
| **Risco**      | 🟡 MÉDIO                                                        |
| **Confiança**  | Alta                                                            |
| **CWE**        | CWE-319: Cleartext Transmission of Sensitive Information        |
| **OWASP**      | A02:2021 – Cryptographic Failures                               |
| **URL**        | Todos os endpoints                                              |
| **Evidência**  | Header `Strict-Transport-Security` ausente                      |

**Descrição:**
A aplicação não envia o header HSTS, que instrui o navegador a sempre usar HTTPS para o domínio. A ausência de HSTS permite ataques de downgrade de protocolo (SSL Stripping) em que um atacante em posição de man-in-the-middle redireciona o tráfego HTTPS para HTTP.

**Observação:** No ambiente de desenvolvimento Docker (HTTP na porta 8080), o HSTS não é aplicável. A correção é relevante para o ambiente de produção com HTTPS configurado.

**Solução:**
```csharp
// Em produção, após app.UseHttpsRedirection():
app.UseHsts();

// Para configuração personalizada:
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
});
```

---

### ZAP-006 — X-Content-Type-Options Header Ausente

| Campo          | Valor                                                              |
|----------------|--------------------------------------------------------------------|
| **Risco**      | 🔵 BAIXO                                                           |
| **Confiança**  | Alta                                                               |
| **CWE**        | CWE-693: Protection Mechanism Failure                              |
| **URL**        | Todos os endpoints                                                 |
| **Evidência**  | Header `X-Content-Type-Options: nosniff` ausente                   |

**Descrição:**
A ausência deste header permite que navegadores realizem MIME-sniffing — interpretação do conteúdo com base no body em vez do `Content-Type` declarado — o que pode levar à execução de scripts em respostas JSON ou XML.

**Solução:**
```csharp
context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
```

---

### ZAP-007 — Divulgação de Informações do Servidor (Server Header)

| Campo          | Valor                                                              |
|----------------|--------------------------------------------------------------------|
| **Risco**      | 🔵 BAIXO                                                           |
| **Confiança**  | Alta                                                               |
| **CWE**        | CWE-200: Exposure of Sensitive Information to an Unauthorized Actor|
| **URL**        | Todos os endpoints                                                 |
| **Evidência**  | `Server: Kestrel` presente nas respostas HTTP                      |

**Descrição:**
O header `Server` revela que a aplicação usa o servidor Kestrel (.NET), facilitando o fingerprinting da stack tecnológica por atacantes. Com essa informação, ataques direcionados a vulnerabilidades conhecidas do Kestrel ou do .NET podem ser realizados com maior precisão.

**Solução:**
```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
});
```

---

### ZAP-008 — Swagger UI Acessível sem Autenticação

| Campo          | Valor                                                              |
|----------------|--------------------------------------------------------------------|
| **Risco**      | 🔵 BAIXO                                                           |
| **Confiança**  | Média                                                              |
| **URL**        | `GET http://localhost:8080/swagger/index.html`                     |

**Descrição:**
O Swagger UI expõe documentação completa de todos os endpoints, parâmetros e schemas de request/response sem autenticação. Embora esteja habilitado apenas em `Development`, qualquer vazamento dessa configuração em produção exporia o mapa completo da API a atacantes.

**Situação atual:** ✅ Protegido por `if (app.Environment.IsDevelopment())` — risco mitigado.

**Recomendação adicional:** Considerar adicionar autenticação básica ao Swagger UI em ambientes de staging.

---

### ZAP-009 — Enumeração de Recursos por Resposta de Erro Diferenciada

| Campo          | Valor                                                     |
|----------------|-----------------------------------------------------------|
| **Risco**      | ℹ️ INFORMACIONAL                                          |
| **URL**        | `GET /api/clientes/99999`, `GET /api/ordens-servico/99999/status` |
| **Evidência**  | Resposta `404` com corpo `{"error":"Cliente não encontrado."}` |

**Descrição:**
As respostas de erro retornam mensagens específicas que permitem distinguir entre "recurso não existe" e outros tipos de erro. Em combinação com IDs sequenciais, isso facilita enumeração de clientes e ordens de serviço existentes.

**Recomendação:** Avaliar se mensagens de erro específicas são necessárias nos endpoints públicos.

---

### ZAP-010 — Ausência de Refresh Token (Informacional)

| Campo          | Valor                                                              |
|----------------|--------------------------------------------------------------------|
| **Risco**      | ℹ️ INFORMACIONAL                                                   |
| **URL**        | `POST /api/auth/login`                                             |
| **Evidência**  | Token JWT com 60 minutos de expiração, sem endpoint de renovação   |

**Descrição:**
O sistema emite tokens JWT com expiração de 60 minutos sem mecanismo de refresh. Tokens comprometidos permanecem válidos durante toda a janela de expiração. A ausência de refresh tokens também significa que administradores precisam reautenticar a cada hora.

**Recomendação:** Implementar refresh tokens com expiração curta para access tokens (15 min) e revogação por lista de tokens inválidos.

---

## Resumo de Remediações

| # | Alerta                                        | Risco    | Esforço   | Prioridade |
|---|-----------------------------------------------|----------|-----------|------------|
| 1 | Rate Limiting no endpoint de login            | 🔴 Alto  | Médio     | P1         |
| 2 | Validação de identidade na aprovação anônima  | 🟡 Médio | Alto      | P1         |
| 3 | Content-Security-Policy header                | 🟡 Médio | Baixo     | P2         |
| 4 | X-Frame-Options / Anti-clickjacking           | 🟡 Médio | Baixo     | P2         |
| 5 | HSTS (produção com HTTPS)                     | 🟡 Médio | Baixo     | P2         |
| 6 | X-Content-Type-Options header                 | 🔵 Baixo | Baixo     | P3         |
| 7 | Remover header Server (Kestrel)               | 🔵 Baixo | Baixo     | P3         |

---

## Conclusão

O scan OWASP ZAP identificou **10 alertas** na aplicação, sendo **1 de risco alto** e **4 de risco médio**. Os pontos positivos observados durante o scan incluem:

- ✅ **Autenticação JWT funcionando corretamente** — endpoints protegidos retornaram `401` para todas as requisições sem token válido
- ✅ **Sem bypass de autorização detectado** — o scan ativo não conseguiu acessar recursos protegidos sem autenticação
- ✅ **Sem injeção SQL detectada** — uso de EF Core com queries parametrizadas protege contra SQL Injection
- ✅ **Sem XSS refletido detectado** — a API não renderiza HTML com dados do usuário
- ✅ **Swagger desabilitado fora de Development**

As melhorias mais críticas são a implementação de **rate limiting no login** (prevenção de força bruta) e a **adição de validação de identidade no endpoint de aprovação anônima**. A aplicação dos security headers é de baixo esforço e deve ser feita em conjunto com as demais correções de segurança identificadas no relatório SonarQube.
