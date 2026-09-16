locals {
  name = "${var.project}-${var.environment}"
}

# Rede provisionada pelo repo infra-k8s (mesma VPC do cluster).
data "terraform_remote_state" "k8s" {
  backend = "s3"
  config = {
    bucket = var.state_bucket
    key    = "infra-k8s/${var.environment}/terraform.tfstate"
    region = var.aws_region
  }
}

# ───────────────────────── Credenciais no Secrets Manager ─────────────────────────
resource "random_password" "db" {
  length  = 24
  special = false # evita caracteres problemáticos em connection strings
}

resource "aws_db_subnet_group" "this" {
  name       = "${local.name}-db-subnets"
  subnet_ids = data.terraform_remote_state.k8s.outputs.private_subnet_ids
}

resource "aws_security_group" "db" {
  name        = "${local.name}-db-sg"
  description = "Acesso ao RDS MySQL"
  vpc_id      = data.terraform_remote_state.k8s.outputs.vpc_id

  # Libera 3306 para dentro da VPC (nós do EKS e ENIs da Lambda).
  ingress {
    description = "MySQL de dentro da VPC"
    from_port   = 3306
    to_port     = 3306
    protocol    = "tcp"
    cidr_blocks = [data.terraform_remote_state.k8s.outputs.vpc_cidr]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

resource "aws_db_parameter_group" "this" {
  name   = "${local.name}-mysql8"
  family = "mysql8.0"

  parameter {
    name  = "character_set_server"
    value = "utf8mb4"
  }
  parameter {
    name  = "collation_server"
    value = "utf8mb4_unicode_ci"
  }
}

resource "aws_db_instance" "this" {
  identifier     = "${local.name}-mysql"
  engine         = "mysql"
  engine_version = "8.0"
  instance_class = var.instance_class

  db_name  = var.db_name
  username = var.db_username
  password = random_password.db.result
  port     = 3306

  allocated_storage    = var.allocated_storage
  storage_type         = var.free_tier ? "gp2" : "gp3"
  storage_encrypted    = true
  multi_az             = var.free_tier ? false : var.multi_az
  db_subnet_group_name = aws_db_subnet_group.this.name
  parameter_group_name = aws_db_parameter_group.this.name

  vpc_security_group_ids = [aws_security_group.db.id]
  publicly_accessible    = false

  # Free Plan não permite retenção de backup; em conta paga usa 7 dias.
  backup_retention_period = var.free_tier ? 0 : 7
  deletion_protection     = var.environment == "prod"
  skip_final_snapshot     = var.environment != "prod"
  apply_immediately       = true
}

# Secret consumido pelo app (.NET/EKS) e pela Lambda de autenticação.
resource "aws_secretsmanager_secret" "db" {
  name = "${local.name}/db-credentials"
}

resource "aws_secretsmanager_secret_version" "db" {
  secret_id = aws_secretsmanager_secret.db.id
  secret_string = jsonencode({
    host     = aws_db_instance.this.address
    port     = aws_db_instance.this.port
    username = var.db_username
    password = random_password.db.result
    dbname   = var.db_name
  })
}
