resource "azurerm_resource_group" "core" {
  for_each = var.regions

  name     = module.regions_config[each.key].names.resource-group
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
