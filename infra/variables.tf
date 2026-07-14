variable "cluster_name" {
  description = "Nome do cluster kind."
  type        = string
  default     = "oficina"
}

variable "worker_count" {
  description = "Quantidade de nós worker no cluster kind."
  type        = number
  default     = 1
}

variable "image_name" {
  description = "Nome/tag da imagem da API a ser carregada no kind."
  type        = string
  default     = "oficina-mecanica-api:local"
}

variable "build_and_load_image" {
  description = "Se verdadeiro, faz docker build da API e carrega a imagem no kind antes do deploy."
  type        = bool
  default     = true
}

variable "apply_manifests" {
  description = "Se verdadeiro, aplica os manifestos de k8s/ após provisionar o cluster."
  type        = bool
  default     = true
}
