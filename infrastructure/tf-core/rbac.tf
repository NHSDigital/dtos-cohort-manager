# This module assigns default base roles to the resources specified
module "global_cohort_rbac" {
  for_each = var.enable_global_rbac ? var.regions : {}

  # This module creates a new managed identity and several role definitions that
  # role up one or more permissions. We'll use those definitions in role assignments
  source = "../../../dtos-devops-templates/infrastructure/modules/managed-identity-global"

  uai_name            = join("-", compact([var.identity_prefix, var.environment, each.key]))
  location            = each.key
  resource_group_name = azurerm_resource_group.core[each.key].name
  environment         = var.environment
  tags                = var.tags
}

resource "azurerm_role_assignment" "global_cohort_uami_role_assignments" {
  for_each = local.resource_id_map

  principal_id = var.rbac_principal_id != null ? var.rbac_principal_id : module.global_cohort_rbac[each.value.region].principal_id
  role_definition_id = lookup(
    module.global_cohort_rbac[each.value.region],
    lookup(local.role_definition_map, each.value.type, "default_role_definition_id")
  )
  scope = each.value.id
}

locals {

  all_resource_ids = flatten([
    for region in keys(var.regions) : concat(
      try([
        for kv in module.key_vault :
        kv.value.region == region ? {
          id     = kv.value.key_vault_id
          type   = "keyvault"
          region = region
        } : []
      ], []),

      try([
        for s in module.storage :
        s.value.region == region ? {
          id     = s.value.storage_account_id
          type   = "store"
          region = region
        } : []
      ], []),

      try([
        for sql in module.azure_sql_server :
        sql.value.region == region ? {
          id     = sql.value.sql_server_id
          type   = "sql"
          region = region
        } : []
      ], []),

      try([
        for fa in module.functionapp :
        fa.value.region == region ? {
          id     = fa.value.id
          type   = "func"
          region = region
        } : []
      ], [])
    )
  ])

  resource_id_map = {
    for res in local.all_resource_ids :
    "${res.region}-${res.type}-${basename(res.id)}" => res
  }

  role_definition_map = {
    keyvault = "keyvault_role_definition_id"
    store    = "storage_role_definition_id"
    sql      = "sql_role_definition_id"
    func     = "function_role_definition_id"
  }
}
