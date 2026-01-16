locals {
  # There are multiple App Service Plans and possibly multiple regions.
  # We cannot nest for loops inside a map, so first iterate all permutations of both as a list of objects...
  container_app_jobs_object_list = flatten([
    for region in keys(var.regions) : [
      for container_app_job, config in var.container_app_jobs.apps : merge(
        {
          region            = region            # 1st iterator
          container_app_job = container_app_job # 2nd iterator
        },
        config, # the rest of the key/value pairs for a specific container_app_job
        {
          env_vars = merge(
            # Add environment variables defined specifically for this container app job:
            config.env_vars_static,

            # Add in the database connection string if the name of the variable is provided:
            config.add_user_assigned_identity != null && length(config.db_connection_string_name) > 0 ? {
              (config.db_connection_string_name) = "Server=tcp:${module.regions_config[region].names.sql-server}.database.windows.net,1433;Initial Catalog=${var.sqlserver.dbs.cohman.db_name_suffix};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication='Active Directory Managed Identity';User ID=${module.user_assigned_managed_identity_sql["${container_app_job}-${region}"].client_id};"
            } : {},

            # Add in the MANAGED_IDENTITY_CLIENT_ID environment variable if using a user assigned managed identity:
            config.add_user_assigned_identity != null ? {
              "MANAGED_IDENTITY_CLIENT_ID" = "${module.user_assigned_managed_identity_sql["${container_app_job}-${region}"].client_id}",
              "TARGET_SUBSCRIPTION_ID"     = var.TARGET_SUBSCRIPTION_ID
            } : {}
          )
        }
      )
    ]
  ])

  # ...then project the list of objects into a map with unique keys (combining the iterators), for consumption by a for_each meta argument
  container_app_jobs_map = {
    for object in local.container_app_jobs_object_list : "${object.container_app_job}-${object.region}" => object
  }
}

module "container-app-job" {
  for_each = local.container_app_jobs_map

  source = "../../../dtos-devops-templates/infrastructure/modules/container-app-job"

  name                = "ca-${lower(each.key)}"
  resource_group_name = azurerm_resource_group.core[each.value.region].name
  location            = each.value.region

  container_app_environment_id = module.container-app-environment["${each.value.container_app_environment_key}-${each.value.region}"].id
  user_assigned_identity_ids   = each.value.add_user_assigned_identity ? [module.user_assigned_managed_identity_sql["${each.key}"].id] : []

  enable_alerting                = var.features.alerts_enabled
  action_group_id                = var.features.alerts_enabled ? module.monitor_action_group_performance[0].monitor_action_group.id : null
  log_analytics_workspace_id     = data.terraform_remote_state.audit.outputs.log_analytics_workspace_id[local.primary_region]
  resource_group_name_monitoring = var.features.alerts_enabled ? azurerm_resource_group.monitoring.name : null

  acr_login_server           = data.azurerm_container_registry.acr.login_server
  acr_managed_identity_id    = each.value.container_registry_use_mi ? data.azurerm_user_assigned_identity.acr_mi.id : null
  docker_image               = "${data.azurerm_container_registry.acr.login_server}/${each.value.docker_image}:${each.value.docker_env_tag != "" ? each.value.docker_env_tag : var.docker_image_tag}"
  memory                     = each.value.memory_in_gb
  replica_retry_limit        = each.value.replica_retry_limit
  replica_timeout_in_seconds = each.value.replica_timeout_in_seconds

  environment_variables = each.value.env_vars != null ? each.value.env_vars : {}

  depends_on = [
    module.azure_sql_server
  ]
}
