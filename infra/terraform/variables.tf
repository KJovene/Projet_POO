variable "app_name" {
  description = "Nom de l'application"
  type        = string
  default     = "locatic"
}

variable "app_image" {
  description = "Image Docker de l'application (construite localement)."
  type        = string
  default     = "locatic:local"
}

variable "app_port" {
  description = "Port d'écoute de Locatic (Kestrel) dans le conteneur."
  type        = number
  default     = 8080
}

variable "web_port" {
  description = "Port externe exposé par le reverse-proxy nginx."
  type        = number
  default     = 8080
}

variable "app_log_level" {
  description = "Niveau de log applicatif."
  type        = string
  default     = "Information"
}

variable "environment" {
  description = "Environnement (dev, prod)"
  type        = string
  default     = "dev"

  validation {
    condition     = contains(["dev", "prod"], var.environment)
    error_message = "L'environnement doit être dev ou prod."
  }
}
