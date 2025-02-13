/* --------------------------------------------------------------------------------------------------
  Clunky way to assign roles to the required service principals (one in non-live, three in live) over
  the main resource group used by this project. This is a temporary solution until we can assign
  roles to the service principals from within a rolled up map object.
-------------------------------------------------------------------------------------------------- */

#### Key Vault Permissions ####

module "rbac_assignments_key_vault_admin" {
  for_each = length(var.rbac_principal_name_key_vault) != 0 ? local.rbac_roles_key_vault_map : {}

  source = "../../../dtos-devops-templates/infrastructure/modules/rbac-assignment"

  principal_id         = data.azuread_group.rbac_principal_key_vault[0].id
  role_definition_name = each.value.role_name
  scope                = azurerm_resource_group.core[each.value.region_key].id
}

data "azuread_group" "rbac_principal_key_vault" {
  count = length(var.rbac_principal_name_key_vault) != 0 ? 1 : 0

  display_name = var.rbac_principal_name_key_vault
}

# First create a map of all roles for all resource groups defined in var.regions:
locals {
  rbac_roles_key_vault_flatlist = flatten([
    for region_key, region_val in var.regions : [
      # Note: using the rbac_roles_key_vault_admin group here:
      for role in local.rbac_roles_key_vault_admin : {
        key        = "${role}-${region_key}"
        role_name  = role
        region_key = region_key
      }
    ]
  ])

  # Project the above list into a map with unique keys for consumption in a for_each meta argument
  rbac_roles_key_vault_map = { for role in local.rbac_roles_key_vault_flatlist : role.key => role }
}

#### Additional Resource Group Permissions ####

module "rbac_assignments_resource_group" {
  for_each = length(var.rbac_principal_name_resource_group) != 0 ? local.rbac_roles_resource_group_map : {}

  source = "../../../dtos-devops-templates/infrastructure/modules/rbac-assignment"

  principal_id         = data.azuread_group.rbac_principal_resource_group[0].id
  role_definition_name = each.value.role_name
  scope                = azurerm_resource_group.core[each.value.region_key].id
}

data "azuread_group" "rbac_principal_resource_group" {
  count = length(var.rbac_principal_name_resource_group) != 0 ? 1 : 0

  display_name = var.rbac_principal_name_resource_group
}

# First create a map of all roles for all resource groups defined in var.regions:
locals {
  rbac_roles_resource_group_flatlist = flatten([
    for region_key, region_val in var.regions : [
      for role in local.rbac_roles_resource_group : {
        key        = "${role}-${region_key}"
        role_name  = role
        region_key = region_key
      }
    ]
  ])

  # Project the above list into a map with unique keys for consumption in a for_each meta argument
  rbac_roles_resource_group_map = { for role in local.rbac_roles_resource_group_flatlist : role.key => role }
}

#### Additional Storage Account Permissions ####

module "rbac_assignments_storage" {
  for_each = length(var.rbac_principal_name_storage) != 0 ? local.rbac_roles_storage_map : {}

  source = "../../../dtos-devops-templates/infrastructure/modules/rbac-assignment"

  principal_id         = data.azuread_group.rbac_principal_storage[0].id
  role_definition_name = each.value.role_name
  scope                = azurerm_resource_group.core[each.value.region_key].id
}

data "azuread_group" "rbac_principal_storage" {
  count = length(var.rbac_principal_name_storage) != 0 ? 1 : 0

  display_name = var.rbac_principal_name_storage
}

# First create a map of all roles for all resource groups defined in var.regions:
locals {
  rbac_roles_storage_flatlist = flatten([
    for region_key, region_val in var.regions : [
      for role in local.rbac_roles_storage : {
        key        = "${role}-${region_key}"
        role_name  = role
        region_key = region_key
      }
    ]
  ])

  # Project the above list into a map with unique keys for consumption in a for_each meta argument
  rbac_roles_storage_map = { for role in local.rbac_roles_storage_flatlist : role.key => role }
}
