# This module assigns default base roles to the resources specified
module "global_cohort_rbac" {
  count = var.enable_global_rbac ? 1: 0

  source = "../../../dtos-devops-templates/infrastructure/modules/rbac-assignment-global"

  identity_prefix = "uami-global"
  environment     = var.environment
  location        = local.primary_region
  resource_group  = azurerm_resource_group.core[local.primary_region].name
  tags            = var.tags

  assignments = []    # (Optional) any additional roles you want to assign
  principal_id = null # (Optional) override principal if delegating to another service identity

  # Da resources wot we want to be secured
  sql_server_ids = local.sql_server_ids
  key_vault_ids = local.key_vault_ids
  storage_ids = local.storage_ids
  function_ids = local.function_ids
}

locals{
  key_vault_ids = try(
    {
      for key, mod in module.key_vault :
        key => { key_vault_id = try(mod.key_vault_id, null) }
        if try(mod.key_vault_id, null) != null
    }, {})

  storage_ids = try(
    {
      for key, mod in module.storage :
        key => { storage_account_id = try(mod.storage_account_id, null) }
        if try(mod.storage_account_id, null) != null
    }, {})

  sql_server_ids = try(
    {
      for key, mod in module.azure_sql_server :
        key => { sql_server_id = try(mod.sql_server_id, null) }
        if try(mod.sql_server_id, null) != null
    }, {})

  function_ids = try(
    {
      for key, mod in module.functionapp :
        key => { function_app_id = try(mod.id, null) }
        if try(mod.id, null) != null
    }, {})
}
