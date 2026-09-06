output "cluster_name" {
  value = module.eks.cluster_name
}

output "cluster_endpoint" {
  value = module.eks.cluster_endpoint
}

output "oidc_provider_arn" {
  value = module.eks.oidc_provider_arn
}

output "vpc_id" {
  value = module.vpc.vpc_id
}

output "vpc_cidr" {
  value = module.vpc.vpc_cidr_block
}

output "private_subnet_ids" {
  value = module.vpc.private_subnets
}

output "public_subnet_ids" {
  value = module.vpc.public_subnets
}

output "nodes_security_group_id" {
  description = "SG dos nós do EKS (usado por RDS e VPC Link)."
  value       = module.eks.node_security_group_id
}

# Consumidos pelo repo oficina-lambda-auth (VPC Link) e oficina-app (TargetGroupBinding).
output "app_listener_arn" {
  description = "ARN do listener do NLB interno (integração VPC Link do API Gateway)."
  value       = aws_lb_listener.app.arn
}

output "app_target_group_arn" {
  description = "ARN do target group ao qual o app se registra via TargetGroupBinding."
  value       = aws_lb_target_group.app.arn
}
