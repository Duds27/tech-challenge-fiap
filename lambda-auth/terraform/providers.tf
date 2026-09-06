terraform {
  required_version = ">= 1.5.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.60"
    }
  }

  # Estado remoto compartilhado (backend S3 + trava DynamoDB).
  # Preencha via `terraform init -backend-config=...` no pipeline.
  backend "s3" {
    key = "lambda-auth/terraform.tfstate"
  }
}

provider "aws" {
  region = var.aws_region
}
