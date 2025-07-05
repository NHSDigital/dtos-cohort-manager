# This module assigns default base roles to the resources specified
module "global_cohort_identity" {
  for_each = var.use_global_rbac_roles ? var.regions : {}

  # This module creates a new managed identity and several role definitions that
  # role up one or more permissions. We'll use those definitions in role assignments
  source = "../../../dtos-devops-templates/infrastructure/modules/managed-identity-roles"

  uai_name            = join("-", compact(["mi-cohort-manager-global", each.key]))
  location            = each.key
  resource_group_name = azurerm_resource_group.core[each.key].name
  environment         = var.environment
  tags                = var.tags
}

resource "azurerm_role_assignment" "global_cohort_mi_role_assignments" {
  for_each = local.resource_id_map

  principal_id = var.rbac_principal_id != null ? var.rbac_principal_id : module.global_cohort_identity[each.value.region].global_mi_principal_id
  role_definition_id = lookup(
    {
      keyvault = module.global_cohort_identity[each.value.region].keyvault_role_definition_id
      store    = module.global_cohort_identity[each.value.region].storage_role_definition_id
      sql      = module.global_cohort_identity[each.value.region].sql_role_definition_id
      func     = module.global_cohort_identity[each.value.region].function_role_definition_id
    },
    each.value.type,
    module.global_cohort_identity[each.value.region].reader_role_id
  )

  scope = each.value.id
}

locals {

  all_resource_ids = flatten([
    for region in keys(var.regions) : concat(
      [
        for kv in module.key_vault :
        {
          id     = kv.key_vault_id
          type   = "keyvault"
          region = region
        }
      ],

      [
        for s in module.storage :
        {
          id     = s.storage_account_id
          type   = "store"
          region = region
        }
      ],

      [
        for sql in module.azure_sql_server :
        {
          id     = sql.sql_server_id
          type   = "sql"
          region = region
        }
      ],

      [
        for fa in module.functionapp :
        {
          id     = fa.id
          type   = "func"
          region = region
        }
      ]
    )
  ])

  # we need to accommodate the validation of names not exceeding 128 characters
  resource_id_map = {
    for res in local.all_resource_ids :
    length(join("-", [res.region, res.type, replace(res.id, "/", "_")])) <= 128 ?
    join("-", [res.region, res.type, replace(res.id, "/", "_")]) :
    join("-", [res.region, res.type, substr(md5(res.id), 0, 8)]) => res
  }
}
