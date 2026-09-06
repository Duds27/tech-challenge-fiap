variable "aws_region" {
  type        = string
  default     = "us-east-1"
  description = "Região AWS."
}

variable "project" {
  type        = string
  default     = "oficina"
  description = "Prefixo de nomeação dos recursos."
}

variable "environment" {
  type        = string
  description = "Ambiente (homolog | prod)."
}

variable "state_bucket" {
  type        = string
  description = "Bucket S3 onde ficam os estados remotos dos repositórios de infra."
}

variable "jwt_secret_id" {
  type        = string
  description = "ARN/nome do secret (Secrets Manager) com a chave do JWT: {\"key\":\"...\"}."
}

variable "jwt_issuer" {
  type    = string
  default = "OficinaMecanicaBackend"
}

variable "jwt_audience" {
  type    = string
  default = "OficinaMecanicaBackend"
}

variable "jwt_expires_minutes" {
  type    = number
  default = 60
}

variable "lambda_zip_path" {
  type        = string
  default     = "../dist/lambda.zip"
  description = "Pacote (zip) gerado pelo esbuild + zip no pipeline."
}
