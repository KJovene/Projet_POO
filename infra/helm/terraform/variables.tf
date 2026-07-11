variable "kubeconfig_path" {
  type    = string
  default = "~/.kube/config"
}

variable "kube_context" {
  type    = string
  default = "minikube"
}

variable "namespace" {
  type    = string
  default = "devops-helm"
}

variable "image_tag" {
  type    = string
  default = "1.0.0"
}

variable "app_replicas" {
  type    = number
  default = 1
}

variable "app_log_level" {
  type    = string
  default = "debug"
}

variable "db_password" {
  type      = string
  sensitive = true
}