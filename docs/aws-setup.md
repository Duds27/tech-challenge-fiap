# Runbook — Provisionar a Infra na AWS (passo a passo)

Cria toda a infraestrutura da Fase 3 na AWS: VPC + EKS, RDS MySQL, Lambda + API Gateway e o
deploy da aplicação. Há dois caminhos:

- **Manual (este guia):** você roda os comandos da sua máquina. Ótimo para a 1ª vez.
- **Automático:** merge em `homolog`/`prod` dispara os workflows (`cd*.yml`). Requer OIDC +
  secrets/variables nos repos — ver [fase3-entrega.md](fase3-entrega.md).

> 💰 **Custo:** EKS + RDS + NAT geram cobrança. Use instâncias pequenas e rode o **teardown**
> (fim do doc) após a avaliação.

---

## 0. Pré-requisitos (uma vez)

Instale: **AWS CLI v2**, **Terraform ≥ 1.5**, **kubectl**, **Docker**, **Node ≥ 20**, **jq**, **git**.

Configure credenciais AWS (usuário/role com permissão de admin para o desafio):

```bash
aws configure           # access key, secret, região (ex.: us-east-1)
aws sts get-caller-identity   # confirma que está autenticado
```

Defina variáveis reutilizadas nos passos (ajuste os valores):

```bash
export AWS_REGION=us-east-1
export ENV=homolog
export TF_STATE_BUCKET=oficina-tfstate-$(aws sts get-caller-identity --query Account --output text)
export TF_LOCK_TABLE=oficina-tf-locks
export NR_LICENSE=""    # chave New Relic (opcional; vazio desabilita o nri-bundle)
```

> ⚠️ **NÃO use o AWS Academy Learner Lab (role `voclabs`).** O boundary dele nega `iam:GetRole`
> e a criação de roles, o que quebra o módulo EKS/IRSA e a Lambda. Use uma **conta AWS pessoal**
> com um usuário IAM `AdministratorAccess`.

> 🪟 **Windows.** Os blocos abaixo são bash. No **CMD**, defina variáveis com `set NOME=valor`
> (uma por linha) e use-as como `%NOME%`; `%NOME%` só expande se já tiver sido definida. No
> **PowerShell**, use `$env:NOME="valor"`. Para evitar erros, prefira passar valores literais
> no `terraform apply` (ex.: `-var="environment=homolog"`).

> 👤 **Múltiplas contas.** Isole as credenciais num perfil: `aws configure --profile pessoal`
> e ative com `set AWS_PROFILE=pessoal` (CMD) / `$env:AWS_PROFILE="pessoal"` (PowerShell).
> Confirme com `aws sts get-caller-identity` (deve mostrar a conta pessoal, não `voclabs`).
> Reaplique o perfil em cada terminal novo.

---

## 1. Bootstrap: estado remoto, ECR e segredo do JWT (uma vez)

**Bucket S3 do estado do Terraform** (com versionamento):

```bash
# us-east-1: SEM LocationConstraint. Outras regiões: adicione
# --create-bucket-configuration LocationConstraint=$AWS_REGION
aws s3api create-bucket --bucket $TF_STATE_BUCKET --region $AWS_REGION
aws s3api put-bucket-versioning --bucket $TF_STATE_BUCKET \
  --versioning-configuration Status=Enabled
```

**Tabela DynamoDB de lock:**

```bash
aws dynamodb create-table --table-name $TF_LOCK_TABLE \
  --attribute-definitions AttributeName=LockID,AttributeType=S \
  --key-schema AttributeName=LockID,KeyType=HASH \
  --billing-mode PAY_PER_REQUEST --region $AWS_REGION
```

**Repositório ECR** (imagem da API):

```bash
aws ecr create-repository --repository-name oficina-mecanica-api --region $AWS_REGION
```

**Segredo do JWT** no Secrets Manager (chave ASCII com ≥ 32 caracteres). O conteúdo
precisa ser **JSON válido**: `{"key":"..."}`.

Linux/macOS (bash):

```bash
aws secretsmanager create-secret --name oficina/jwt \
  --secret-string '{"key":"TROQUE_POR_UMA_CHAVE_ASCII_COM_32+_CHARS"}' \
  --region $AWS_REGION
```

> 🪟 **Windows (CMD/PowerShell):** NÃO use aspas simples estilo bash — o shell corrompe o
> JSON (vira `'{key:...}'` inválido) e a Lambda quebra com `SyntaxError` no `JSON.parse`.
> Grave a partir de um arquivo, montando o JSON com `ConvertTo-Json`:
>
> ```powershell
> $key = "TROQUE_POR_UMA_CHAVE_ASCII_COM_32+_CHARS"
> (@{ key = $key } | ConvertTo-Json -Compress) | Set-Content -Path jwt.json -Encoding ascii
> aws secretsmanager create-secret --name oficina/jwt --secret-string file://jwt.json --region $env:AWS_REGION
> Remove-Item jwt.json
> ```
>
> Se o secret já existir (foi criado errado), troque `create-secret` por
> `put-secret-value --secret-id oficina/jwt`. Confira com:
> `aws secretsmanager get-secret-value --secret-id oficina/jwt --query SecretString --output text`
> (deve imprimir `{"key":"..."}`).

---

## 2. Rede + cluster: `infra-k8s` (VPC + EKS)

> É o **primeiro** — cria a VPC que os demais reutilizam. ~15–20 min.

```bash
cd infra-k8s
terraform init \
  -backend-config="bucket=$TF_STATE_BUCKET" \
  -backend-config="key=infra-k8s/$ENV/terraform.tfstate" \
  -backend-config="region=$AWS_REGION" \
  -backend-config="dynamodb_table=$TF_LOCK_TABLE"
terraform apply \
  -var="environment=$ENV" \
  -var="aws_region=$AWS_REGION" \
  -var="newrelic_license_key=$NR_LICENSE"
```

Confira os outputs (usados adiante): `terraform output`.

---

## 3. Banco: `infra-database` (RDS MySQL)

> Lê o estado do `infra-k8s` para achar a VPC/subnets. ~10 min.

```bash
cd ../infra-database
terraform init \
  -backend-config="bucket=$TF_STATE_BUCKET" \
  -backend-config="key=infra-database/$ENV/terraform.tfstate" \
  -backend-config="region=$AWS_REGION" \
  -backend-config="dynamodb_table=$TF_LOCK_TABLE"
terraform apply \
  -var="environment=$ENV" \
  -var="aws_region=$AWS_REGION" \
  -var="state_bucket=$TF_STATE_BUCKET"
```

Cria o RDS e publica as credenciais no secret `oficina-$ENV/db-credentials`.

---

## 4. Aplicação no EKS (`oficina-app`)

> Faça o app **antes** de testar login: ele roda as **migrations** na subida e cria a tabela
> `Clientes` que a Lambda consulta.

Colete os valores (da raiz do monorepo `oficina-app`):

```bash
cd ..
export ACCOUNT=$(aws sts get-caller-identity --query Account --output text)
export ECR=$ACCOUNT.dkr.ecr.$AWS_REGION.amazonaws.com/oficina-mecanica-api
export CLUSTER=$(terraform -chdir=infra-k8s output -raw cluster_name)
export TG_ARN=$(terraform -chdir=infra-k8s output -raw app_target_group_arn)
export DB_SECRET=$(terraform -chdir=infra-database output -raw db_secret_arn)
```

Build e push da imagem para o ECR:

```bash
aws ecr get-login-password --region $AWS_REGION \
  | docker login --username AWS --password-stdin $ACCOUNT.dkr.ecr.$AWS_REGION.amazonaws.com
docker build -t $ECR:latest OficinaMecanicaBackend
docker push $ECR:latest
```

Kubeconfig do EKS + Secret da aplicação (a partir do Secrets Manager):

```bash
aws eks update-kubeconfig --name $CLUSTER --region $AWS_REGION

DB=$(aws secretsmanager get-secret-value --secret-id $DB_SECRET --query SecretString --output text)
JWT=$(aws secretsmanager get-secret-value --secret-id oficina/jwt --query SecretString --output text)
CONN="Server=$(echo $DB|jq -r .host);Port=$(echo $DB|jq -r .port);Database=$(echo $DB|jq -r .dbname);User=$(echo $DB|jq -r .username);Password=$(echo $DB|jq -r .password);"

kubectl create namespace oficina --dry-run=client -o yaml | kubectl apply -f -
kubectl -n oficina create secret generic api-secrets \
  --from-literal=ConnectionStrings__DefaultConnection="$CONN" \
  --from-literal=Jwt__Key="$(echo $JWT|jq -r .key)" \
  --from-literal=NEW_RELIC_LICENSE_KEY="$NR_LICENSE" \
  --dry-run=client -o yaml | kubectl apply -f -
```

Deploy via kustomize (overlay AWS):

```bash
cd k8s/overlays/aws
kustomize edit set image oficina-mecanica-api=$ECR:latest
sed -i "s|__APP_TARGET_GROUP_ARN__|$TG_ARN|g" targetgroupbinding.yaml
kubectl apply -k .
kubectl -n oficina rollout status deployment/api --timeout=300s
cd ../../..
```

---

## 5. Autenticação: `lambda-auth` (Lambda + API Gateway)

Build do bundle Node + zip, depois Terraform:

```bash
cd lambda-auth
npm ci && npm run build
(cd dist && zip -r ../dist/lambda.zip handler.js authorizer.js *.map)

cd terraform
terraform init \
  -backend-config="bucket=$TF_STATE_BUCKET" \
  -backend-config="key=lambda-auth/$ENV/terraform.tfstate" \
  -backend-config="region=$AWS_REGION" \
  -backend-config="dynamodb_table=$TF_LOCK_TABLE"
terraform apply \
  -var="environment=$ENV" \
  -var="aws_region=$AWS_REGION" \
  -var="state_bucket=$TF_STATE_BUCKET" \
  -var="jwt_secret_id=oficina/jwt"

export API_URL=$(terraform output -raw api_endpoint)
cd ../..
```

---

## 6. Semear um cliente e testar

A rota `/api/*` (inclusive o login admin) exige JWT, então o **primeiro** cliente é inserido
direto no banco por um pod efêmero dentro do cluster (que enxerga o RDS):

```bash
DB=$(aws secretsmanager get-secret-value --secret-id $DB_SECRET --query SecretString --output text)
kubectl -n oficina run mysql-cli --rm -it --image=mysql:8 --restart=Never -- \
  mysql -h $(echo $DB|jq -r .host) -u $(echo $DB|jq -r .username) -p$(echo $DB|jq -r .password) \
  $(echo $DB|jq -r .dbname) \
  -e "INSERT INTO Clientes (CpfCnpj, Nome, Ativo, DataCriacao) VALUES ('52998224725','Cliente Teste',1,UTC_TIMESTAMP());"
```

Teste ponta a ponta:

```bash
# 1) autentica por CPF → recebe o token
TOKEN=$(curl -s -X POST "$API_URL/auth" -H 'content-type: application/json' \
  -d '{"cpf":"529.982.247-25"}' | jq -r .token)
echo "$TOKEN"

# 2) rota protegida sem token → 401 ; com token → 200
curl -s -o /dev/null -w "%{http_code}\n" "$API_URL/api/clientes"
curl -s "$API_URL/api/clientes" -H "Authorization: Bearer $TOKEN"
```

---

## 7. Observabilidade (New Relic)

- Se passou `NR_LICENSE` no passo 2, o `nri-bundle` já coleta CPU/memória/uptime.
- Importe o dashboard: `docs/observability/newrelic-dashboard.json` (ajuste `accountId`).
- Alertas: `cd docs/observability/terraform && terraform init && terraform apply` (provider `newrelic`).

### 7.1. Observabilidade no Free Tier (t3.micro) — estratégia recomendada

O `nri-bundle` sobe muitos pods (daemonset de infra por nó + kube-state-metrics + coletores)
e **não cabe** em 2× t3.micro — o Helm estoura o timeout (`context deadline exceeded`) e deixa
o release em `failed`. Em vez de brigar com isso:

1. **Mantenha o `nri-bundle` desligado** no `infra-k8s` (`-var="newrelic_license_key="`).
   Se ele já subiu quebrado, remova: `terraform apply ... -var="newrelic_license_key="`
   (ou `kubectl delete namespace newrelic --wait=false`).

2. **Use o APM do app (.NET)** — in-process, sem pods extras. Cobre o essencial do enunciado
   (latência, throughput, erros, traces distribuídos e logs JSON com `CorrelationId`):

   ```powershell
   $nr = "SUA_LICENSE_KEY_NEW_RELIC"
   (@{ stringData = @{ NEW_RELIC_LICENSE_KEY = $nr } } | ConvertTo-Json -Compress) | Set-Content -Encoding ascii nrpatch.json
   kubectl -n oficina patch secret api-secrets --type merge --patch-file nrpatch.json
   Remove-Item nrpatch.json
   kubectl -n oficina rollout restart deployment/api
   ```

3. **CPU/memória do K8s** (para o vídeo): use o `metrics-server` já instalado —
   `kubectl top pods -n oficina` e `kubectl top nodes`. Cobre o requisito sem o nri-bundle.

4. **Se precisar mesmo das métricas de infra via nri-bundle** (ex.: só para gravar): suba
   nós e ligue temporariamente, destruindo depois:
   `terraform apply ... -var="newrelic_license_key=$nr" -var="node_desired_size=4"`.
   Em conta paga com `t3.small`+ isso deixa de ser problema.

> Regra prática: **APM (app) para os dashboards de latência/erros/logs** + **`kubectl top`
> para CPU/memória**. O nri-bundle é opcional no free tier.

---

## 8. Teardown (destruir para não gerar custo)

Ordem **inversa** do provisionamento:

```bash
# app (opcional)
kubectl delete -k k8s/overlays/aws || true

cd lambda-auth/terraform && terraform destroy -var="environment=$ENV" -var="aws_region=$AWS_REGION" -var="state_bucket=$TF_STATE_BUCKET" -var="jwt_secret_id=oficina/jwt" && cd ../..
cd infra-database && terraform destroy -var="environment=$ENV" -var="aws_region=$AWS_REGION" -var="state_bucket=$TF_STATE_BUCKET" && cd ..
cd infra-k8s && terraform destroy -var="environment=$ENV" -var="aws_region=$AWS_REGION" -var="newrelic_license_key=$NR_LICENSE" && cd ..
```

Bucket S3 de estado, tabela DynamoDB, ECR e o secret do JWT permanecem (remova manualmente se quiser).

---

## Ordem-resumo

`bootstrap` → **infra-k8s** → **infra-database** → **oficina-app (deploy)** → **lambda-auth** → semear cliente → testar.

## Solução de problemas

| Sintoma | Causa provável | Ação |
| --- | --- | --- |
| `terraform init` pede backend | faltou `-backend-config` | repasse os 4 `-backend-config` do passo. |
| `Requested AMI for this version X is not supported` (node group) | versão do K8s fora de suporte (sem AMIs) | use uma versão em `STANDARD_SUPPORT` (`aws eks describe-cluster-versions`); ajuste `kubernetes_version`. Se o cluster já subiu numa versão velha, **destroy + apply** (o EKS não pula minor no upgrade). |
| `AccessDenied ... iam:GetRole on role voclabs` | AWS Academy Learner Lab | use conta pessoal com IAM `AdministratorAccess` (ver avisos do passo 0). |
| `not eligible for Free Tier` (EC2/node group) | conta no AWS Free Plan | use `t3.micro` (`node_instance_types`) + prefix delegation na VPC CNI; ou faça upgrade para plano pago. |
| `FreeTierRestrictionError` (RDS) | conta no AWS Free Plan | mantenha `free_tier = true` no `infra-database` (single-AZ, `backup_retention_period = 0`, gp2, `db.t3.micro`). |
| `FailedScheduling ... Too many pods` | teto de pods/nó do t3.micro (4) | prefix delegation + `node_max_pods` (nodeadm) no `infra-k8s`; ou mais nós (`node_desired_size`). |
| helm `newrelic` `context deadline exceeded` / release `failed` | nri-bundle pesado demais p/ t3.micro | desligue (`newrelic_license_key=""`) e use o **APM do app** + `kubectl top` (ver 7.1). |
| Lambda `/auth` 500 + log `SyntaxError ... JSON.parse` | secret `oficina/jwt` com JSON inválido (aspas do Windows) | regrave o secret via `file://` (ver passo 1, aviso Windows); depois sincronize `api-secrets.Jwt__Key`. |
| Lambda `/auth` retorna 500 | schema ainda não migrado / sem cliente | garanta o passo 4 (app) e o passo 6 (seed). |
| `/api/*` sempre 401 | token ausente/inválido ou authorizer | confira o `Authorization: Bearer` e a chave do JWT (mesmo secret). |
| Pods `CrashLoopBackOff` | connection string do RDS incorreta | revise o Secret `api-secrets` e o SG do RDS. |
| API Gateway 503 em `/api/*` | pods não registrados no target group | confira o `TargetGroupBinding` e o AWS LB Controller. |
| Alvos `unhealthy` `Target.FailedHealthChecks` (pod Ready) | SG dos nós não libera o health check do NLB na 8080 | `node_security_group_additional_rules` no `infra-k8s` (ingress 8080 da CIDR da VPC). |
