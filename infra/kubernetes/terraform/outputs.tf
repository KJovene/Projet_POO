output "namespace" {
  description = "Namespace préparé pour l'application."
  value       = kubernetes_namespace_v1.app.metadata[0].name
}

output "sqlite_pvc_name" {
  description = "Nom du PVC SQLite à monter par le chart Helm."
  value       = kubernetes_persistent_volume_claim_v1.sqlite.metadata[0].name
}

output "sqlite_mount_path" {
  description = "Répertoire de montage du volume SQLite."
  value       = var.sqlite_mount_path
}

output "sqlite_connection_string" {
  description = "Chaîne de connexion .NET à injecter dans l'application."
  value       = "Data Source=${local.sqlite_db_path}"
}

# Commande Helm prête à l'emploi consommant les ressources préparées par Terraform.
output "helm_deploy_command" {
  description = "Déploiement de l'app sur l'infra préparée."
  value = join(" ", [
    "helm upgrade --install ${var.app_name} ../../helm/devops-app-chart",
    "-n ${kubernetes_namespace_v1.app.metadata[0].name}",
    "-f ../../helm/values.yaml -f ../../helm/values-${var.environment}.yaml",
    "--set persistence.existingClaim=${kubernetes_persistent_volume_claim_v1.sqlite.metadata[0].name}",
    "--set config.connectionString='Data Source=${local.sqlite_db_path}'",
  ])
}

# Contrat de handoff destiné à l'étape suivante (Ansible) :
#   terraform output -json handoff > ../ansible/handoff.json
output "handoff" {
  description = "Informations nécessaires à Ansible pour poursuivre l'installation."
  value = {
    kube_context   = var.kube_context
    namespace      = kubernetes_namespace_v1.app.metadata[0].name
    app_name       = var.app_name
    environment    = var.environment
    sqlite_pvc     = kubernetes_persistent_volume_claim_v1.sqlite.metadata[0].name
    sqlite_path    = local.sqlite_db_path
    chart_path     = "../../helm/devops-app-chart"
    values_files   = ["../../helm/values.yaml", "../../helm/values-${var.environment}.yaml"]
  }
}
