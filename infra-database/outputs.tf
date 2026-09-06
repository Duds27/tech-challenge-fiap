output "rds_endpoint" {
  description = "Endpoint (host) do RDS MySQL."
  value       = aws_db_instance.this.address
}

output "rds_port" {
  value = aws_db_instance.this.port
}

output "db_secret_arn" {
  description = "ARN do secret com as credenciais do banco (host/port/user/pass/dbname)."
  value       = aws_secretsmanager_secret.db.arn
}

output "db_security_group_id" {
  value = aws_security_group.db.id
}
