# Fase 3 — Guia de Split de Repositórios e Entrega

Este monorepo contém, em pastas separadas, o conteúdo dos **4 repositórios** exigidos.
Abaixo, como separá-los, proteger as branches e montar a entrega.

## Mapeamento pasta → repositório

| Pasta neste monorepo | Repositório destino | Papel |
| --- | --- | --- |
| `OficinaMecanicaBackend/`, `k8s/`, `docs/`, `.github/workflows/{ci,cd-aws}.yml` | **oficina-app** | App .NET + manifestos K8s + docs |
| `lambda-auth/` | **oficina-lambda-auth** | Function de auth por CPF + API Gateway |
| `infra-k8s/` | **oficina-infra-k8s** | Terraform VPC + EKS |
| `infra-database/` | **oficina-infra-database** | Terraform RDS MySQL |

> A documentação arquitetural (`docs/architecture`, `docs/observability`) vive no
> `oficina-app` e é referenciada pelos READMEs dos demais repositórios.

## Como separar (preservando o essencial)

Para cada novo repositório, crie-o vazio no GitHub e publique o conteúdo da pasta:

```bash
# Exemplo: oficina-lambda-auth
cd lambda-auth
git init -b main
git add .
git commit -m "Fase 3: função de autenticação por CPF"
git remote add origin git@github.com:<org>/oficina-lambda-auth.git
git push -u origin main
```

Repita para `infra-k8s`, `infra-database`. O `oficina-app` pode ser o histórico atual
(remova as pastas migradas antes de finalizar): `git rm -r lambda-auth infra-k8s infra-database`.

> ⚠️ **Crie cada repositório VAZIO no GitHub** — NÃO marque "Add a README/.gitignore/license".
> Se o repo já tiver um commit inicial, o `git push` falha com `! [rejected] ... (fetch first)`
> porque o push não é _fast-forward_. Soluções: (a) recriar o repo vazio; (b) `git push --force`
> (só em repo recém-criado, sobrescreve o README autogerado); (c) preservar o commit inicial com
> `git fetch <repo> main` + `git merge --allow-unrelated-histories FETCH_HEAD` antes do push.

### Alternativa: split preservando histórico por pasta

Gera um branch cuja **raiz** é o conteúdo da pasta e o envia ao repo correspondente. **Atenção:
cada pasta vai para o SEU repo** (não troque o destino):

```bash
git subtree split -P lambda-auth    -b split-lambda-auth
git push https://github.com/<org>/oficina-lambda-auth.git    split-lambda-auth:main

git subtree split -P infra-k8s      -b split-infra-k8s
git push https://github.com/<org>/oficina-infra-k8s.git      split-infra-k8s:main

git subtree split -P infra-database -b split-infra-database
git push https://github.com/<org>/oficina-infra-database.git split-infra-database:main
```

## Regras de proteção de branch (nos 4 repositórios)

Em _Settings → Branches → Add rule_ para `main`:

- ✅ Require a pull request before merging (sem commits diretos).
- ✅ Require status checks to pass (selecionar o job de CI).
- ✅ Require branches to be up to date.
- (Opcional) Require approvals ≥ 1.

Fluxo de ambientes: branches **`homolog`** e **`prod`** disparam o deploy automático
(workflows `cd*.yml`). Crie os _Environments_ `homolog` e `prod` em _Settings → Environments_
e cadastre os secrets/variables abaixo.

## Segredos e variáveis (GitHub Actions)

Comuns (Variables): `AWS_REGION`, `TF_STATE_BUCKET`, `TF_LOCK_TABLE`.
Comuns (Secrets): `AWS_DEPLOY_ROLE_ARN` (role assumida via OIDC).

| Repositório | Adicionais |
| --- | --- |
| oficina-app | Vars: `ECR_REPOSITORY`, `EKS_CLUSTER_NAME`, `APP_TARGET_GROUP_ARN`, `DB_SECRET_ID`, `JWT_SECRET_ID`. Secret: `NEW_RELIC_LICENSE_KEY`. |
| oficina-lambda-auth | Secret: `JWT_SECRET_ID`. |
| oficina-infra-k8s | Secret: `NEW_RELIC_LICENSE_KEY`. |
| oficina-infra-database | — |

## Pré-requisitos de nuvem (uma vez)

1. Conta AWS + **OIDC provider** para GitHub Actions e role de deploy.
2. Bucket **S3** de estado + tabela **DynamoDB** de lock.
3. Secret do JWT no **Secrets Manager**: `{"key":"<chave ASCII ≥ 32 chars>"}`.
4. Repositório **ECR** para a imagem da API.
5. Conta **New Relic** + license key + user key (para os alertas via Terraform).

## Ordem de provisionamento

`infra-k8s` (VPC+EKS) → `infra-database` (RDS) → `oficina-lambda-auth` (Lambda+API GW) →
`oficina-app` (deploy no EKS).

## Checklist de entrega (Portal do Aluno — PDF único)

- [ ] Links dos **4 repositórios**.
- [ ] Link do **vídeo (≤ 15 min)**: autenticação por CPF, pipeline CI/CD, deploy automático,
      consumo das APIs protegidas, dashboard New Relic ao vivo, logs/traces.
- [ ] Links das **documentações** (`docs/architecture`, `docs/observability`, Swagger/Postman).
- [ ] Confirmação do usuário **`soat-architecture`** adicionado como colaborador nos 4 repos
      (_Settings → Collaborators_).
- [ ] Cada README com: propósito, tecnologias, execução/deploy, diagrama e link Swagger/Postman.
