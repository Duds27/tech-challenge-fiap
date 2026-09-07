terraform {
  required_version = ">= 1.5.0"
  required_providers {
    newrelic = {
      source  = "newrelic/newrelic"
      version = "~> 3.43"
    }
  }
}

provider "newrelic" {
  account_id = var.newrelic_account_id
  api_key    = var.newrelic_api_key # NerdGraph User key
  region     = var.newrelic_region  # US | EU
}

variable "newrelic_account_id" { type = number }
variable "newrelic_api_key" {
  type      = string
  sensitive = true
}
variable "newrelic_region" {
  type    = string
  default = "US"
}
variable "app_name" {
  type    = string
  default = "oficina-mecanica-api"
}
variable "notification_channel_ids" {
  type    = list(string)
  default = []
}

# ───────────────────────────── Política de alertas ─────────────────────────────
resource "newrelic_alert_policy" "oficina" {
  name = "Oficina Mecânica — Fase 3"
}

# Latência das APIs (p95 > 1s por 5 min).
resource "newrelic_nrql_alert_condition" "latencia" {
  policy_id          = newrelic_alert_policy.oficina.id
  name               = "Latência alta das APIs (p95)"
  type               = "static"
  enabled            = true
  aggregation_window = 60

  nrql {
    query = "SELECT percentile(duration, 95) FROM Transaction WHERE appName = '${var.app_name}'"
  }

  critical {
    operator              = "above"
    threshold             = 1.0
    threshold_duration    = 300
    threshold_occurrences = "all"
  }
}

# Falha no processamento de ordens de serviço.
resource "newrelic_nrql_alert_condition" "os_falha" {
  policy_id = newrelic_alert_policy.oficina.id
  name      = "Falha no processamento de OS"
  type      = "static"
  enabled   = true

  nrql {
    query = "SELECT count(*) FROM Log WHERE Evento = 'os_falha_processamento'"
  }

  critical {
    operator              = "above"
    threshold             = 0
    threshold_duration    = 300
    threshold_occurrences = "at_least_once"
  }
}

# Saturação de CPU dos pods (> 85% do request por 10 min).
resource "newrelic_nrql_alert_condition" "cpu" {
  policy_id = newrelic_alert_policy.oficina.id
  name      = "CPU alta nos pods do EKS"
  type      = "static"
  enabled   = true

  nrql {
    query = "SELECT average(cpuCoresUtilization) FROM K8sContainerSample WHERE clusterName LIKE 'oficina-%'"
  }

  critical {
    operator              = "above"
    threshold             = 85
    threshold_duration    = 600
    threshold_occurrences = "all"
  }
}
