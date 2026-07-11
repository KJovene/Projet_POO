#!/usr/bin/env bash
# Génère l'inventaire Ansible à partir des sorties Terraform (infra/terraform).
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
terraform_dir="$script_dir/../../terraform"
output_file="$script_dir/../inventory.yml"

terraform -chdir="$terraform_dir" output -raw ansible_inventory > "$output_file"
echo "Inventaire écrit dans $output_file"
ansible-inventory -i "$output_file" --graph
