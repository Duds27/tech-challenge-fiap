# Infraestrutura como Código (Terraform) — cluster Kubernetes local (kind)

Provisiona um cluster **Kubernetes local com kind**, instala o **metrics-server**
(pré-requisito do HPA), constrói/carrega a imagem da API e aplica os manifestos de
[`/k8s`](../k8s) — incluindo o banco de dados **MySQL** (StatefulSet).

## Recursos criados

| Recurso Terraform | O que provisiona |
| ----------------- | ---------------- |
| `kind_cluster.this` | Cluster kind (`control-plane` + N `worker`). |
| `helm_release.metrics_server` | metrics-server no `kube-system` (com `--kubelet-insecure-tls`, necessário no kind) para alimentar o HPA. |
| `null_resource.build_and_load_image` | `docker build` da API e `kind load docker-image` para dentro do cluster. |
| `null_resource.apply_manifests` | `kubectl apply -k ../k8s` (Namespace, ConfigMap, Secret, MySQL, API, HPA). |

## Pré-requisitos

- [Terraform](https://developer.hashicorp.com/terraform/downloads) >= 1.3
- [Docker](https://www.docker.com/) em execução
- [kind](https://kind.sigs.k8s.io/) e [kubectl](https://kubernetes.io/docs/tasks/tools/) no PATH

## Aplicar

```bash
cd infra
terraform init
terraform plan
terraform apply    # cria o cluster, instala metrics-server, builda a imagem e faz o deploy
```

Ao final, acesse a API:

```bash
kubectl -n oficina rollout status deploy/api
kubectl -n oficina port-forward svc/api 8080:80
# http://localhost:8080/swagger
```

## Variáveis úteis

| Variável | Padrão | Descrição |
| -------- | ------ | --------- |
| `cluster_name` | `oficina` | Nome do cluster kind. |
| `worker_count` | `1` | Número de nós worker. |
| `image_name` | `oficina-mecanica-api:local` | Tag da imagem da API. |
| `build_and_load_image` | `true` | Builda e carrega a imagem no kind. Defina `false` se a imagem já vier de um registry. |
| `apply_manifests` | `true` | Aplica os manifestos após criar o cluster. |

## Destruir

```bash
terraform destroy
```

## Troubleshooting

### `node(s) already exist for a cluster with the name "oficina"`

Já existe um cluster kind com esse nome (de um `apply`/`kind create` anterior que não
foi destruído). Os nós do kind são containers Docker; remova o cluster órfão e refaça:

```bash
kind get clusters                        # confirma que "oficina" existe
kind delete cluster --name oficina       # remove o cluster (e seus containers)
docker ps -a --filter "name=oficina"     # opcional: confira se sobraram containers
terraform apply                          # recria do zero
```

> Se o `terraform apply` reclamar que o recurso já está no state, rode `terraform destroy`
> antes (após o `kind delete`, ele só acerta o state). Para evitar o problema, sempre
> finalize com `terraform destroy` antes de um novo `apply`.

### Reiniciar os pods (rollout restart)

Recria os pods sem alterar os manifestos — útil após carregar uma nova imagem no kind
ou para forçar a releitura de ConfigMap/Secret:

```bash
kubectl -n oficina rollout restart deployment/api      # reinicia a API (rolling)
kubectl -n oficina rollout status deployment/api       # acompanha o rollout
kubectl -n oficina rollout restart statefulset/mysql   # reinicia o MySQL (se necessário)
```

Alternativas pontuais:

```bash
kubectl -n oficina delete pod <nome-do-pod>            # recria só um pod (o Deployment sobe outro)
kubectl -n oficina scale deployment/api --replicas=0   # derruba tudo...
kubectl -n oficina scale deployment/api --replicas=2   # ...e sobe de novo
```

> Após buildar uma imagem nova, carregue-a no kind antes de reiniciar:
> `docker build -t oficina-mecanica-api:local OficinaMecanicaBackend` →
> `kind load docker-image oficina-mecanica-api:local --name oficina` →
> `kubectl -n oficina rollout restart deployment/api`.

### HPA com `TARGETS = <unknown>/70%`

O metrics-server ainda está iniciando ou não coletou métricas. Verifique:

```bash
kubectl -n kube-system get deployment metrics-server   # deve estar 1/1
```

### Pod da API em `CrashLoopBackOff`

Normalmente o MySQL ainda está subindo. O `Program.cs` tem retry de conexão no startup;
acompanhe os logs e aguarde o `mysql-0` ficar `Ready`:

```bash
kubectl -n oficina get pods
kubectl -n oficina logs deployment/api
```

> **Nota sobre cloud:** este módulo mira um ambiente **local (kind)**, conforme
> escolhido para a Fase 2. Para nuvem (ex.: EKS/AKS/GKE), trocam-se os providers
> `kind` + `metrics-server` por um módulo de cluster gerenciado e um banco gerenciado
> (ex.: RDS/Cloud SQL), mantendo os mesmos manifestos de `/k8s`.
