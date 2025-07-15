# This module assigns default role definitions and permissions to the "global" principal and
# associated resources.
module "global_cohort_identity" {
  for_each = var.use_global_rbac_roles ? var.regions : {}

  source = "../../../dtos-devops-templates/infrastructure/modules/managed-identity"

  uai_name            = join("-", compact(["mi-cohort-manager-global", each.key]))
  location            = each.key
  resource_group_name = azurerm_resource_group.core[each.key].name
  tags                = var.tags
}

# Assign all the custom role definitions to the Main Identity created above
module "global_cohort_identity_roles" {
  for_each = var.use_global_rbac_roles ? var.regions : {}

  source = "../../../dtos-devops-templates/infrastructure/modules/managed-identity-roles"

  assignable_scopes = [local.role_assignment_scope_id]
  role_scope_id     = azurerm_resource_group.core[each.key].id
  location          = each.key
  environment       = var.environment
  tags              = var.tags
}

# Now loop through all resources we be interested in and create role
# assignments between our custom role definitions and the principal id(s)
resource "azurerm_role_assignment" "global_cohort_mi_role_assignments" {
  for_each = var.use_global_rbac_roles ? local.resource_id_map: {}

  # name = join("-", [
  #   each.value.id,
  #   local.get_role_local.get_definition_id[each.key],
  #   sha1(coalesce(var.rbac_principal_id, module.global_cohort_identity[each.value.region].principal_id))
  # ])

  principal_id = coalesce(
    # The user-supplied principal_id takes precedence
    var.rbac_principal_id,

    module.global_cohort_identity[each.value.region].principal_id
  )

  role_definition_id = lookup(
    {
      keyvault = module.global_cohort_identity_roles[each.value.region].keyvault_role_definition_id
      store    = module.global_cohort_identity_roles[each.value.region].storage_role_definition_id
      sql      = module.global_cohort_identity_roles[each.value.region].sql_role_definition_id
      func     = module.global_cohort_identity_roles[each.value.region].function_role_definition_id
    },
    each.value.type,

    # If we could not find a match, just default to the Reader role
    module.global_cohort_identity_roles[each.value.region].reader_role_id
  )

  scope = each.value.id
}

output "assigned_roles_by_region" {
  value = {
    for k, v in azurerm_role_assignment.global_cohort_mi_role_assignments :
    local.resource_id_map[k].region => {
      "${local.resource_id_map[k].type}" = {
        role  = v.role_definition_id
        scope = v.scope
      }
    }
  }
}
locals {
  role_assignment_scope_id = "/subscriptions/${data.azurerm_client_config.current.subscription_id}"

  all_resource_ids = flatten([
    for region in keys(var.regions) : concat(
      [for kv in module.key_vault :
        {
          id     = kv.key_vault_id
          type   = "keyvault"
          region = region
        }
      ],

      [for s in module.storage :
        {
          id     = s.storage_account_id
          type   = "store"
          region = region
        }
      ],

      [for sql in module.azure_sql_server :
        {
          id     = sql.sql_server_id
          type   = "sql"
          region = region
        }
      ],

      [for fa in module.functionapp :
        {
          id     = fa.id
          type   = "func"
          region = region
        }
      ]
    )
  ])

  resource_id_map = {
    for res in local.all_resource_ids :
      join("-", [res.region, res.type, replace(res.id, "/", "_")]) => res
  }

  # These roles are here to maintain existing rbac role assignment behaviour
  # for use by the respective resource module's "rbac.tf" module.
  # ============================
  rbac_storage_roles = var.use_global_rbac_roles ? [] : [
    "Storage Account Contributor",
    "Storage Blob Data Owner",
    "Storage Table Data Contributor",
    "Storage Queue Data Contributor"
  ]

  rbac_key_vault_roles = var.use_global_rbac_roles ? [] : [
    "Key Vault Certificate User",
    "Key Vault Crypto User",
    "Key Vault Secrets User"
  ]
  # ============================
}
