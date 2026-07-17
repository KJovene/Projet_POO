terraform {
  required_version = ">= 1.5"
  required_providers {
    helm = {
      source  = "hashicorp/helm"
      version = "~> 2.17"
    }
  }
}

provider "helm" {
  kubernetes {
    config_path    = pathexpand(var.kubeconfig_path)
    config_context = var.kube_context
  }
}

resource "helm_release" "devops_app" {
  name             = "devops-app"
  chart            = "${path.module}/../devops-app-chart"
  namespace        = var.namespace
  create_namespace = true

  values = [
    file("${path.module}/../values-dev.yaml")
  ]

  set {
    name  = "image.tag"
    value = var.image_tag
  }

  set {
    name  = "replicaCount"
    value = var.app_replicas
  }

  set {
    name  = "config.appLogLevel"
    value = var.app_log_level
  }

  set_sensitive {
    name  = "secret.dbPassword"
    value = var.db_password
  }
}

output "port_forward_command" {
  value = "kubectl port-forward -n ${var.namespace} svc/devops-app-svc 18080:80"
}

output "runtime_contract" {
  value = {
    runtime       = "kubernetes-helm"
    release       = helm_release.devops_app.name
    namespace     = var.namespace
    replicas      = var.app_replicas
    app_log_level = var.app_log_level
    database_host = "postgres-svc"
  }
}