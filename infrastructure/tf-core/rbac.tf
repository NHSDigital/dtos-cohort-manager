# This module assigns default base roles to the resources specified
module "global_cohort_rbac" {
  for_each = var.enable_global_rbac ? var.regions : {}

  source = "../../../dtos-devops-templates/infrastructure/modules/managed-identity-global"

  uai_name            = join("-", compact([var.identity_prefix, var.environment, each.key]))
  location            = each.key
  resource_group_name = azurerm_resource_group.core[each.key].name
  environment         = var.environment
  tags                = var.tags

  resource_ids = local.all_resource_ids
}

locals {
  key_vault_ids = try([
    for _, mod in module.key_vault : mod.key_vault_id
    if try(mod.key_vault_id, null) != null
  ], [])

  storage_ids = try([
    for _, mod in module.storage : mod.storage_account_id
    if try(mod.storage_account_id, null) != null
  ], [])

  sql_server_ids = try([
    for _, mod in module.azure_sql_server : mod.sql_server_id
    if try(mod.sql_server_id, null) != null
  ], [])

  function_ids = try([
    for _, mod in module.functionapp : mod.id
    if try(mod.id, null) != null
  ], [])

  all_resource_ids = concat(
    local.key_vault_ids,
    local.storage_ids,
    local.sql_server_ids,
    local.function_ids
  )
}
