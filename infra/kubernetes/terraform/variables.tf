variable "kubeconfig_path" {
  type        = string
  description = "Chemin du kubeconfig."
  default     = "~/.kube/config"
}

variable "kube_context" {
  type        = string
  description = "Contexte kubectl à utiliser (ex. minikube)."
  default     = "minikube"
}

variable "environment" {
  type        = string
  description = "Environnement cible (dev, prod)."
  default     = "dev"

  validation {
    condition     = contains(["dev", "prod"], var.environment)
    error_message = "environment doit valoir 'dev' ou 'prod'."
  }
}

variable "namespace" {
  type        = string
  description = "Namespace explicite. Si vide, dérivé en 'locatic-<environment>'."
  default     = ""
}

variable "app_name" {
  type        = string
  description = "Nom logique de l'application."
  default     = "locatic"
}

variable "sqlite_pvc_name" {
  type        = string
  description = "Nom du PersistentVolumeClaim pour la base SQLite."
  default     = "locatic-sqlite"
}

variable "sqlite_storage" {
  type        = string
  description = "Taille du volume SQLite."
  default     = "1Gi"
}

variable "sqlite_mount_path" {
  type        = string
  description = "Répertoire de montage du volume SQLite dans le conteneur."
  default     = "/app/data"
}

variable "sqlite_db_file" {
  type        = string
  description = "Nom du fichier de base SQLite."
  default     = "locatic.db"
}

variable "storage_class" {
  type        = string
  description = "StorageClass à utiliser. Vide = classe par défaut du cluster (minikube : standard)."
  default     = ""
}
