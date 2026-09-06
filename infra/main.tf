locals {
  repo_root = "${path.module}/.."
  k8s_dir   = "${path.module}/../k8s"
  app_dir   = "${path.module}/../OficinaMecanicaBackend"
}

# ─────────────────────────── Cluster kind ───────────────────────────
resource "kind_cluster" "this" {
  name           = var.cluster_name
  wait_for_ready = true

  kind_config {
    kind        = "Cluster"
    api_version = "kind.x-k8s.io/v1alpha4"

    node {
      role = "control-plane"
    }

    dynamic "node" {
      for_each = range(var.worker_count)
      content {
        role = "worker"
      }
    }
  }
}

# ─────────── metrics-server (necessário para o HPA no kind) ───────────
resource "helm_release" "metrics_server" {
  name       = "metrics-server"
  repository = "https://kubernetes-sigs.github.io/metrics-server/"
  chart      = "metrics-server"
  namespace  = "kube-system"
  version    = "3.12.1"

  # No kind os certificados do kubelet são autoassinados; permite a coleta de métricas.
  set {
    name  = "args[0]"
    value = "--kubelet-insecure-tls"
  }

  depends_on = [kind_cluster.this]
}

# ─────────── Build da imagem da API e carga no cluster kind ───────────
resource "null_resource" "build_and_load_image" {
  count = var.build_and_load_image ? 1 : 0

  triggers = {
    cluster = kind_cluster.this.name
  }

  provisioner "local-exec" {
    command = "docker build -t ${var.image_name} ${local.app_dir} && kind load docker-image ${var.image_name} --name ${var.cluster_name}"
  }

  depends_on = [kind_cluster.this]
}

# ─────────── Aplicação dos manifestos (API + MySQL + HPA) ───────────
resource "null_resource" "apply_manifests" {
  count = var.apply_manifests ? 1 : 0

  triggers = {
    cluster   = kind_cluster.this.name
    manifests = filesha256("${local.k8s_dir}/overlays/local/kustomization.yaml")
  }

  provisioner "local-exec" {
    command = "kubectl apply -k ${local.k8s_dir}/overlays/local"
    environment = {
      KUBECONFIG = kind_cluster.this.kubeconfig_path
    }
  }

  depends_on = [
    kind_cluster.this,
    helm_release.metrics_server,
    null_resource.build_and_load_image,
  ]
}
