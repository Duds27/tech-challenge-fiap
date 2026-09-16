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

variable "state_bucket" {
  type        = string
  description = "Bucket S3 com os estados remotos (para ler os outputs do infra-k8s)."
}

variable "db_name" {
  type    = string
  default = "OficinaMecanica"
}

variable "db_username" {
  type    = string
  default = "oficina_admin"
}

variable "instance_class" {
  type    = string
  default = "db.t3.micro"
}

variable "allocated_storage" {
  type    = number
  default = 20
}

variable "multi_az" {
  type        = bool
  default     = true
  description = "Alta disponibilidade (failover automático). Ignorado quando free_tier = true."
}

variable "free_tier" {
  type        = bool
  default     = true
  description = "Ajusta o RDS aos limites do AWS Free Plan: single-AZ, sem retenção de backup e storage gp2. Em conta paga, defina false para HA e backups."
}
