
module "container-app" {
  for_each = local.container_apps_map

  source = "../../../dtos-devops-templates/infrastructure/modules/container-app"

  providers = {
    azurerm     = azurerm
    azurerm.hub = azurerm.hub
  }

  name                = "ca-${lower(each.key)}"
  resource_group_name = azurerm_resource_group.core[each.value.region].name
  location            = each.value.region

  container_app_environment_id = module.container-app-environment["${each.value.container_app_environment_key}-${each.value.region}"].id
  user_assigned_identity_ids   = each.value.add_user_assigned_identity ? [module.user_assigned_managed_identity_sql["${each.key}"].id] : []

  acr_login_server        = data.azurerm_container_registry.acr.login_server
  acr_managed_identity_id = each.value.container_registry_use_mi ? data.azurerm_user_assigned_identity.acr_mi.id : null
  docker_image            = "${data.azurerm_container_registry.acr.login_server}/${each.value.docker_image}:${each.value.docker_env_tag != "" ? each.value.docker_env_tag : var.docker_image_tag}"

  environment_variables = each.value.env_vars != null ? each.value.env_vars : {}

  is_tcp_app = each.value.is_tcp_app
  is_web_app = each.value.is_web_app
  port       = each.value.port

  infra_key_vault_rg   = each.value.infra_key_vault_rg
  infra_key_vault_name = each.value.infra_key_vault_name

  depends_on = [
    module.azure_sql_server
  ]
}

locals {
  # There are multiple App Service Plans and possibly multiple regions.
  # We cannot nest for loops inside a map, so first iterate all permutations of both as a list of objects...
  container_apps_object_list = flatten([
    for region in keys(var.regions) : [
      for container_app, config in var.container_apps.apps : merge(
        {
          region        = region        # 1st iterator
          container_app = container_app # 2nd iterator
        },
        config, # the rest of the key/value pairs for a specific container_app
        {
          env_vars = merge(
            # Add environment variables defined specifically for this container app :
            config.env_vars_static,

            # Add in the database connection string if the name of the variable is provided:
            config.add_user_assigned_identity != null && length(config.db_connection_string_name) > 0 ? {
              (config.db_connection_string_name) = "Server=tcp:${module.regions_config[region].names.sql-server}.database.windows.net,1433;Initial Catalog=${var.sqlserver.dbs.cohman.db_name_suffix};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication='Active Directory Managed Identity';User ID=${module.user_assigned_managed_identity_sql["${container_app}-${region}"].client_id};"
            } : {},

            # Add in the MANAGED_IDENTITY_CLIENT_ID environment variable if using a user assigned managed identity:
            config.add_user_assigned_identity != false ? {
              "MANAGED_IDENTITY_CLIENT_ID" = "${module.user_assigned_managed_identity_sql["${container_app}-${region}"].client_id}",
              "TARGET_SUBSCRIPTION_ID"     = var.TARGET_SUBSCRIPTION_ID
            } : {}
          )
        }
      )
    ]
  ])

  # ...then project the list of objects into a map with unique keys (combining the iterators), for consumption by a for_each meta argument
  container_apps_map = {
    for object in local.container_apps_object_list : "${object.container_app}-${object.region}" => object
  }
}
