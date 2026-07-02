# Déploiement local via Docker : conteneur Locatic (Kestrel) derrière un
# reverse-proxy nginx. Nginx est provisionné "nu" ; sa configuration proxy_pass
# est posée ensuite par Ansible (voir infra/ansible).
terraform {
  required_version = ">= 1.6"
  required_providers {
    docker = {
      source  = "kreuzwerker/docker"
      version = "~> 3.0"
    }
  }
}

provider "docker" {}

locals {
  app_container = "${var.app_name}-${var.environment}-app"
  web_container = "${var.app_name}-${var.environment}-web"
  upstream      = "${var.app_name}-${var.environment}-app:${var.app_port}"
}

# --- Réseau applicatif (DNS interne : nginx résout le conteneur app par son nom) ---
resource "docker_network" "app_network" {
  name = "${var.app_name}-network"
}

# --- Application Locatic (image construite au préalable : docker build -t locatic:local ./Locatic) ---
resource "docker_image" "app" {
  name         = var.app_image
  keep_locally = true
}

resource "docker_container" "app" {
  name  = local.app_container
  image = docker_image.app.image_id

  # Non publié sur l'hôte : l'accès passe par le reverse-proxy nginx.
  networks_advanced {
    name = docker_network.app_network.name
  }

  env = [
    "ASPNETCORE_URLS=http://+:${var.app_port}",
    "ASPNETCORE_ENVIRONMENT=${var.environment == "prod" ? "Production" : "Development"}",
  ]
}

# --- Reverse-proxy nginx (config appliquée par Ansible) ---
resource "docker_image" "nginx" {
  name         = "nginx:alpine"
  keep_locally = true
}

resource "docker_container" "web" {
  name  = local.web_container
  image = docker_image.nginx.image_id

  # L'app doit exister avant nginx (résolution DNS de l'upstream au démarrage).
  depends_on = [docker_container.app]

  ports {
    internal = 80
    external = var.web_port
  }

  networks_advanced {
    name = docker_network.app_network.name
  }

  # Config reverse-proxy injectée dans le conteneur : nginx démarre déjà
  # configuré, et la config est réappliquée à chaque terraform apply.
  upload {
    file = "/etc/nginx/conf.d/default.conf"
    content = templatefile("${path.module}/templates/nginx-proxy.conf.tftpl", {
      upstream    = local.upstream
      app_name    = var.app_name
      environment = var.environment
    })
  }
}

output "web_url" {
  description = "URL publique (reverse-proxy nginx -> Locatic)."
  value       = "http://localhost:${var.web_port}"
}

output "app_container" {
  value = local.app_container
}

# Inventaire consommé par Ansible (render-inventory.sh) pour configurer le proxy.
output "ansible_inventory" {
  description = "Inventaire Ansible des serveurs web à configurer."
  value = yamlencode({
    all = {
      children = {
        webservers = {
          hosts = {
            (local.web_container) = {
              ansible_connection         = "docker"
              ansible_python_interpreter = "/usr/bin/python3"
              public_port                = var.web_port
              public_url                 = "http://localhost:${var.web_port}"
              nginx_upstream             = local.upstream
            }
          }
        }
      }
      vars = {
        app_name        = var.app_name
        app_environment = var.environment
        app_log_level   = var.app_log_level
      }
    }
  })
}
