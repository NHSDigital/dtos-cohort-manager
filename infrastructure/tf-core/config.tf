resource "azurerm_resource_group" "core" {
  for_each = var.regions

  name     = module.regions_config[each.key].names.resource-group
  location = each.key

  lifecycle {
    ignore_changes = [tags]
  }
}

#### Test code: ####

locals {
  flattened_roles = {
    for region_name, region in var.regions : region_name => {
      for principal_type, principal in var.rbac_principals : "${principal.key}-${principal.principal_display_name}" => {
        roles = flatten([
          principal_type == "keyvault" ? principal.roles : [],
          principal_type == "resource_group" ? principal.roles : [],
          principal_type == "storage_account" ? principal.roles : []
        ])
      }
    }
  }
}

output "flattened_roles" {
  value = local.flattened_roles
}




# Add a role assignment to the audit resource group for each role defined in locals:
# module "rbac_assignments" {
#   for_each = local.rbac_roles_resource_groups_map

#   source = "../../../dtos-devops-templates/infrastructure/modules/rbac-assignment"

#   principal_id         = data.azuread_group.rbac_principal.id
#   role_definition_name = each.value.role_name
#   scope                = azurerm_resource_group.core[each.value.region_key].id
# }

# data "azuread_group" "rbac_principal" {
#   for_each = var.rbac_principals

#   display_name = each.value.display_name
# }

# # First create a map of all roles for all resource groups defined in var.regions:
# locals {
#   rbac_roles_resource_groups_flatlist = flatten([
#     for region_key, region_val in var.regions : [
#       for principal_key, principal_val in var.rbac_principals : {
#         for role in principal_val.roles : {
#         #key        = "${role}-${region_key}"
#         role_name  = principal_key
#         rbac_principal = data.azuread_group.rbac_principal[role]
#         region_key = region_key
#         }
#       }
#     ]
#   ])

#   # Project the above list into a map with unique keys for consumption in a for_each meta argument
#   rbac_roles_resource_groups_map = { for role in local.rbac_roles_resource_groups_flatlist : role.key => role }
# }

module "regions_config" {
  for_each = var.regions

  source = "../../../dtos-devops-templates/infrastructure/modules/shared-config"

  location    = each.key
  application = var.application
  env         = var.environment
  tags        = var.tags
}
