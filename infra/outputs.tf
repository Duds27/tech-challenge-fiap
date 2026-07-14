output "cluster_name" {
  description = "Nome do cluster kind provisionado."
  value       = kind_cluster.this.name
}

output "kubeconfig_path" {
  description = "Caminho do kubeconfig gerado para o cluster."
  value       = kind_cluster.this.kubeconfig_path
}

output "kube_context" {
  description = "Contexto kubectl do cluster kind."
  value       = "kind-${kind_cluster.this.name}"
}

output "endpoint" {
  description = "Endpoint da API do Kubernetes."
  value       = kind_cluster.this.endpoint
}

output "acesso_api" {
  description = "Como acessar a API após o deploy."
  value       = "kubectl -n oficina port-forward svc/api 8080:80  =>  http://localhost:8080/swagger"
}
