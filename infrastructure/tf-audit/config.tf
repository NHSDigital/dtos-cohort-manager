resource "azurerm_resource_group" "audit" {
  for_each = { for key, val in var.regions : key => val if val.is_primary_region }

  name     = "${module.regions_config[each.key].names.resource-group}-audit"
  location = each.key

  lifecycle {
    ignore_changes = [tags]
  }
}

module "regions_config" {
  for_each = var.regions

  source = "../../../dtos-devops-templates/infrastructure/modules/shared-config"

  location    = each.key
  application = var.application
  env         = var.environment
  tags        = var.tags
}
