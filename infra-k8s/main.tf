locals {
  name = "${var.project}-${var.environment}"
  tags = {
    Project     = var.project
    Environment = var.environment
    ManagedBy   = "terraform"
  }
}

data "aws_availability_zones" "available" {
  state = "available"
}

# ───────────────────────────────── VPC ─────────────────────────────────
module "vpc" {
  source  = "terraform-aws-modules/vpc/aws"
  version = "~> 5.13"

  name = "${local.name}-vpc"
  cidr = var.vpc_cidr
  azs  = slice(data.aws_availability_zones.available.names, 0, 2)

  private_subnets = [cidrsubnet(var.vpc_cidr, 4, 0), cidrsubnet(var.vpc_cidr, 4, 1)]
  public_subnets  = [cidrsubnet(var.vpc_cidr, 4, 2), cidrsubnet(var.vpc_cidr, 4, 3)]

  enable_nat_gateway   = true
  single_nat_gateway   = true # 1 NAT reduz custo (aceitável fora de produção crítica)
  enable_dns_hostnames = true

  # Tags exigidas pelo AWS Load Balancer Controller.
  public_subnet_tags  = { "kubernetes.io/role/elb" = "1" }
  private_subnet_tags = { "kubernetes.io/role/internal-elb" = "1" }

  tags = local.tags
}

# ───────────────────────────────── EKS ─────────────────────────────────
module "eks" {
  source  = "terraform-aws-modules/eks/aws"
  version = "~> 20.24"

  cluster_name    = local.name
  cluster_version = var.kubernetes_version

  cluster_endpoint_public_access = true
  enable_irsa                    = true

  vpc_id     = module.vpc.vpc_id
  subnet_ids = module.vpc.private_subnets

  # Addons gerenciados. Prefix delegation na VPC CNI eleva o limite de pods/nó —
  # essencial em instâncias pequenas (t3.micro só teria ~4 IPs/pods sem isso).
  # before_compute garante que a config vale desde o primeiro nó.
  cluster_addons = {
    vpc-cni = {
      before_compute              = true
      most_recent                 = true
      resolve_conflicts_on_create = "OVERWRITE"
      configuration_values = jsonencode({
        env = {
          ENABLE_PREFIX_DELEGATION = "true"
          WARM_PREFIX_TARGET       = "1"
        }
      })
    }
    kube-proxy = { most_recent = true }
    coredns = {
      most_recent = true
      # 1 réplica economiza recursos no free tier (sem HA de DNS).
      configuration_values = jsonencode({ replicaCount = 1 })
    }
  }

  eks_managed_node_groups = {
    default = {
      # Família de AMI atual do EKS (Amazon Linux 2023). AL2 foi descontinuada.
      ami_type       = "AL2023_x86_64_STANDARD"
      instance_types = var.node_instance_types
      min_size       = var.node_min_size
      max_size       = var.node_max_size
      desired_size   = var.node_desired_size

      # Eleva o max-pods do kubelet via NodeConfig do nodeadm (AL2023). Sem isso, o
      # t3.micro fica preso em 4 pods/nó ("Too many pods"), mesmo com prefix delegation.
      cloudinit_pre_nodeadm = [{
        content_type = "application/node.eks.aws"
        content = yamlencode({
          apiVersion = "node.eks.aws/v1alpha1"
          kind       = "NodeConfig"
          spec = {
            kubelet = {
              config = {
                maxPods = var.node_max_pods
              }
            }
          }
        })
      }]
    }
  }

  # Libera o health check + tráfego do NLB interno até os pods na porta 8080.
  # Sem esta regra, o SG dos nós bloqueia o health check e os alvos ficam "unhealthy"
  # (API Gateway devolve 503). O NLB fica na VPC, então liberamos a CIDR da VPC.
  node_security_group_additional_rules = {
    nlb_to_pods_8080 = {
      description = "NLB health check e trafego para os pods da API"
      protocol    = "tcp"
      from_port   = 8080
      to_port     = 8080
      type        = "ingress"
      cidr_blocks = [var.vpc_cidr]
    }
  }

  # Concede ao criador do cluster acesso admin (kubectl/CI).
  enable_cluster_creator_admin_permissions = true

  tags = local.tags
}

# ───────────── NLB interno para integração via VPC Link (API Gateway) ─────────────
# O app expõe um Service ClusterIP; um TargetGroupBinding (no repo oficina-app) registra
# os pods como alvos IP deste target group. O API Gateway integra via VPC Link → este NLB.
resource "aws_lb" "app" {
  name               = "${local.name}-app-nlb"
  internal           = true
  load_balancer_type = "network"
  subnets            = module.vpc.private_subnets
  tags               = local.tags
}

resource "aws_lb_target_group" "app" {
  name        = "${local.name}-app-tg"
  port        = 80
  protocol    = "TCP"
  target_type = "ip"
  vpc_id      = module.vpc.vpc_id

  health_check {
    protocol = "HTTP"
    path     = "/health/ready"
    port     = "traffic-port"
  }

  tags = local.tags
}

resource "aws_lb_listener" "app" {
  load_balancer_arn = aws_lb.app.arn
  port              = 80
  protocol          = "TCP"

  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.app.arn
  }
}
