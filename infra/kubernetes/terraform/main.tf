terraform {
  required_version = ">= 1.5"
  required_providers {
    kubernetes = {
      source  = "hashicorp/kubernetes"
      version = "~> 2.38"
    }
  }
  # État local (gitignoré). Pour un état partagé, déclarer ici un backend distant
  # (ex. "s3", "gcs", "kubernetes") — voir README.
}

provider "kubernetes" {
  config_path    = pathexpand(var.kubeconfig_path)
  config_context = var.kube_context
}

locals {
  # Namespace = valeur explicite sinon dérivé de l'environnement (locatic-dev/prod).
  namespace = var.namespace != "" ? var.namespace : "locatic-${var.environment}"

  common_labels = {
    "app.kubernetes.io/part-of"    = var.app_name
    "app.kubernetes.io/managed-by" = "terraform"
    "environment"                  = var.environment
  }

  sqlite_db_path = "${var.sqlite_mount_path}/${var.sqlite_db_file}"
}

# 1) Namespace de l'application.
resource "kubernetes_namespace_v1" "app" {
  metadata {
    name   = local.namespace
    labels = local.common_labels
  }
}

# 2) Stockage persistant pour la base SQLite (monté ensuite par le chart Helm).
resource "kubernetes_persistent_volume_claim_v1" "sqlite" {
  metadata {
    name      = var.sqlite_pvc_name
    namespace = kubernetes_namespace_v1.app.metadata[0].name
    labels    = local.common_labels
  }

  spec {
    access_modes       = ["ReadWriteOnce"]
    storage_class_name = var.storage_class != "" ? var.storage_class : null
    resources {
      requests = {
        storage = var.sqlite_storage
      }
    }
  }

  # On prépare le volume sans attendre qu'un pod le consomme (Helm le montera ensuite).
  wait_until_bound = false
}
