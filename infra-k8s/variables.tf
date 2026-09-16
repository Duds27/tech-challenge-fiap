variable "aws_region" {
  type    = string
  default = "us-east-1"
}

variable "project" {
  type    = string
  default = "oficina"
}

variable "environment" {
  type        = string
  description = "Ambiente (homolog | prod)."
}

variable "vpc_cidr" {
  type    = string
  default = "10.20.0.0/16"
}

variable "kubernetes_version" {
  # Use uma versão em suporte no EKS (as antigas perdem as AMIs dos nós).
  # Verifique as disponíveis com:
  #   aws eks describe-cluster-versions --query "clusterVersions[?status=='STANDARD_SUPPORT'].clusterVersion" --output table
  type    = string
  default = "1.36"
}

variable "node_instance_types" {
  # t3.micro é free-tier-eligible (contas no Free Plan bloqueiam tipos maiores).
  # Requer prefix delegation na VPC CNI (ver cluster_addons no main.tf) para caber
  # os pods. Em conta paga, prefira t3.small/t3.medium.
  type    = list(string)
  default = ["t3.micro"]
}

variable "node_min_size" {
  type    = number
  default = 2
}

variable "node_max_size" {
  type    = number
  default = 5
}

variable "node_desired_size" {
  type    = number
  default = 2
}

variable "node_max_pods" {
  # Eleva o teto de pods/nó (o t3.micro vem com 4). Requer prefix delegation na VPC CNI
  # (já habilitado em cluster_addons). ~34 é o teto teórico do t3.micro; 17 dá folga
  # segura — na prática a memória (1 GiB) é o limite real.
  type    = number
  default = 17
}

variable "newrelic_license_key" {
  type        = string
  default     = ""
  description = "Chave de licença do New Relic para o nri-bundle (vazio desabilita o release)."
  sensitive   = true
}
