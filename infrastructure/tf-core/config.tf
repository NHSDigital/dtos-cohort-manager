resource "azurerm_resource_group" "core" {
  for_each = var.regions

  name     = module.regions_config[each.key].names.resource-group
  location = each.key

  lifecycle {
    ignore_changes = [tags]
  }
}

data "azuread_group" "rbac_principal" {
  display_name = var.rbac_principal_name
}

# First create a map of all roles for all resource groups defined in var.regions:
locals {
  rbac_roles_resource_groups_flatlist = flatten([
    for region_key, region_val in var.regions : [
      for role in local.rbac_roles_resource_group : {
        key        = "${role}-${region_key}"
        role_name = role
        region_key = region_key
      }
    ]
  ])

  # Project the above list into a map with unique keys for consumption in a for_each meta argument
  rbac_roles_resource_groups_map = { for role in local.rbac_roles_resource_groups_flatlist : role.key => role }
}

module "regions_config" {
  for_each = var.regions

  source = "../../../dtos-devops-templates/infrastructure/modules/shared-config"

  location    = each.key
  application = var.application
  env         = var.environment
  tags        = var.tags
}
