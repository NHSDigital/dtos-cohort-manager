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

  assignable_scopes = [var.role_assignment_scope_id]
  location          = each.key
  environment       = var.environment
  tags              = var.tags
}

# Now loop through all resources we be interested in and create role
# assignments between our custom role definitions and the principal id(s)
resource "azurerm_role_assignment" "global_cohort_mi_role_assignments" {
  for_each = var.use_global_rbac_roles ? local.resource_id_map : {}

  name = join("-", [
    each.value.id,
    local.get_role_definition_id[each.key],
    sha1(coalesce(var.rbac_principal_id, module.global_cohort_identity[each.value.region].principal_id))
  ])
  principal_id = coalesce(
    # The user-supplied principal_id takes precedence
    var.rbac_principal_id,

    module.global_cohort_identity[each.value.region].principal_id
  )

  role_definition_id = local.get_role_definition_id[each.key]
  scope = each.value.id

  # We wanna ensure that all providers know that we need roles BEFORE assignments.
  # It's for safety.
  depends_on = [module.global_cohort_identity_roles]
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
  all_resource_ids = flatten([
    for region in keys(var.regions) : concat(
      [for kv in try(module.key_vault, []) :
        {
          id     = kv.key_vault_id
          type   = "keyvault"
          region = region
        }
      ],

      [for s in try(module.storage, []) :
        {
          id     = s.storage_account_id
          type   = "store"
          region = region
        }
      ],

      [for sql in try(module.azure_sql_server, []) :
        {
          id     = sql.sql_server_id
          type   = "sql"
          region = region
        }
      ],

      [for fa in try(module.functionapp, []) :
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

  role_definition_ids_by_type = {
    for region in keys(var.regions) : region => {
      keyvault = module.global_cohort_identity_roles[region].keyvault_role_definition_id
      store    = module.global_cohort_identity_roles[region].storage_role_definition_id
      sql      = module.global_cohort_identity_roles[region].sql_role_definition_id
      func     = module.global_cohort_identity_roles[region].function_role_definition_id
      grid     = module.global_cohort_identity_roles[region].grid_role_definition_id
      bus      = module.global_cohort_identity_roles[region].bus_role_definition_id
      default = module.global_cohort_identity_roles[region].reader_role_id
    }
  }

  get_role_definition_id = {
    for k, v in local.resource_id_map : k => lookup(
      local.role_definition_ids_by_type[v.region],
      v.type,
      local.role_definition_ids_by_type[v.region].default
    )
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
