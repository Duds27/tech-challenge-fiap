# ───────────────────────── metrics-server (HPA) ─────────────────────────
resource "helm_release" "metrics_server" {
  name       = "metrics-server"
  repository = "https://kubernetes-sigs.github.io/metrics-server/"
  chart      = "metrics-server"
  namespace  = "kube-system"
  version    = "3.12.1"

  depends_on = [module.eks]
}

# ───────────────────── AWS Load Balancer Controller ─────────────────────
# IRSA role com a policy do controller (gerencia NLB/ALB e TargetGroupBinding).
module "lb_controller_irsa" {
  source  = "terraform-aws-modules/iam/aws//modules/iam-role-for-service-accounts-eks"
  version = "~> 5.44"

  role_name                              = "${local.name}-lb-controller"
  attach_load_balancer_controller_policy = true

  oidc_providers = {
    main = {
      provider_arn               = module.eks.oidc_provider_arn
      namespace_service_accounts = ["kube-system:aws-load-balancer-controller"]
    }
  }

  tags = local.tags
}

resource "helm_release" "aws_lb_controller" {
  name       = "aws-load-balancer-controller"
  repository = "https://aws.github.io/eks-charts"
  chart      = "aws-load-balancer-controller"
  namespace  = "kube-system"
  version    = "1.8.1"

  set {
    name  = "clusterName"
    value = module.eks.cluster_name
  }
  set {
    name  = "serviceAccount.create"
    value = "true"
  }
  set {
    name  = "serviceAccount.name"
    value = "aws-load-balancer-controller"
  }
  set {
    name  = "serviceAccount.annotations.eks\\.amazonaws\\.com/role-arn"
    value = module.lb_controller_irsa.iam_role_arn
  }
  set {
    name  = "region"
    value = var.aws_region
  }
  set {
    name  = "vpcId"
    value = module.vpc.vpc_id
  }

  depends_on = [module.eks, helm_release.metrics_server]
}

# ───────────────────── New Relic Kubernetes integration ─────────────────────
# Coleta CPU/memória/uptime/eventos do cluster. Só é instalado se a licença for informada.
resource "helm_release" "newrelic" {
  count = var.newrelic_license_key != "" ? 1 : 0

  name             = "newrelic-bundle"
  repository       = "https://helm-charts.newrelic.com"
  chart            = "nri-bundle"
  namespace        = "newrelic"
  create_namespace = true
  version          = "5.0.99"

  set {
    name  = "global.licenseKey"
    value = var.newrelic_license_key
  }
  set {
    name  = "global.cluster"
    value = local.name
  }
  set {
    name  = "newrelic-infrastructure.privileged"
    value = "true"
  }
  set {
    name  = "kube-state-metrics.enabled"
    value = "true"
  }
  set {
    name  = "nri-kube-events.enabled"
    value = "true"
  }
  set {
    name  = "logging.enabled"
    value = "true"
  }

  depends_on = [module.eks]
}
