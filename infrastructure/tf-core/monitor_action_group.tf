resource "azurerm_resource_group" "monitoring" {
  name     = "${module.regions_config[local.primary_region].names.resource-group}-monitoring"
  location = local.primary_region

  tags = local.merged_tags
}

# module "monitor_action_group_performance" {
#   count = var.features.alerts_enabled ? 1 : 0

#   source = "../../../dtos-devops-templates/infrastructure/modules/monitor-action-group"

#   name                = "${module.regions_config[local.primary_region].names.monitor-action-group}-perf"
#   resource_group_name = azurerm_resource_group.monitoring.name
#   location            = local.primary_region
#   short_name          = "COHMAN"
#   email_receiver = {
#     email = {
#       name          = "email"
#       email_address = data.azurerm_key_vault_secret.monitoring_email_address[local.primary_region].value
#     }
#   }
# }
