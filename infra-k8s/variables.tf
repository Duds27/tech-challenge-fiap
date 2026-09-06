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
  type    = string
  default = "1.30"
}

variable "node_instance_types" {
  type    = list(string)
  default = ["t3.medium"]
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

variable "newrelic_license_key" {
  type        = string
  default     = ""
  description = "Chave de licença do New Relic para o nri-bundle (vazio desabilita o release)."
  sensitive   = true
}
