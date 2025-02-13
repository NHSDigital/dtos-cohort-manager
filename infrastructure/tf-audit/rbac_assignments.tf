# Add a role assignment to the audit resource group for each role defined in locals:
module "rbac_assignments" {
  for_each = length(var.rbac_principal_name_resource_group) != 0 ? local.rbac_roles_resource_groups_map : {}

  source = "../../../dtos-devops-templates/infrastructure/modules/rbac-assignment"

  principal_id         = data.azuread_group.rbac_principal.id
  role_definition_name = each.value.role_name
  scope                = azurerm_resource_group.audit[each.value.region_key].id
}

data "azuread_group" "rbac_principal" {
  display_name = var.rbac_principal_name_resource_group
}

# First create a map of all roles for all resource groups defined in var.regions:
locals {
  rbac_roles_resource_groups_flatlist = flatten([
    for region_key, region_val in var.regions : [
      for role in local.rbac_roles_resource_group : {
        key        = "${role}-${region_key}"
        role_name  = role
        region_key = region_key
      }
    ]
  ])

  # Project the above list into a map with unique keys for consumption in a for_each meta argument
  rbac_roles_resource_groups_map = { for role in local.rbac_roles_resource_groups_flatlist : role.key => role }
}
