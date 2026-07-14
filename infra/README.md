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

> **Nota sobre cloud:** este módulo mira um ambiente **local (kind)**, conforme
> escolhido para a Fase 2. Para nuvem (ex.: EKS/AKS/GKE), trocam-se os providers
> `kind` + `metrics-server` por um módulo de cluster gerenciado e um banco gerenciado
> (ex.: RDS/Cloud SQL), mantendo os mesmos manifestos de `/k8s`.
