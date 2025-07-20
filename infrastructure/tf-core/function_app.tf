module "functionapp" {
  for_each = local.function_app_map

  source = "../../../dtos-devops-templates/infrastructure/modules/function-app"

  function_app_name   = "${module.regions_config[each.value.region].names.function-app}-${lower(each.value.name_suffix)}"
  resource_group_name = azurerm_resource_group.core[each.value.region].name
  location            = each.value.region

  app_settings = each.value.app_settings

  log_analytics_workspace_id                           = data.terraform_remote_state.audit.outputs.log_analytics_workspace_id[local.primary_region]
  monitor_diagnostic_setting_function_app_enabled_logs = local.monitor_diagnostic_setting_function_app_enabled_logs
  monitor_diagnostic_setting_function_app_metrics      = local.monitor_diagnostic_setting_function_app_metrics

  public_network_access_enabled = var.features.public_network_access_enabled
  vnet_integration_subnet_id    = module.subnets["${module.regions_config[each.value.region].names.subnet}-apps"].id

  # We use the RBAC module to set default roles
  # rbac_role_assignments = each.value.rbac_role_assignments
  rbac_role_assignments = []

  asp_id = module.app-service-plan["${each.value.app_service_plan_key}-${each.value.region}"].app_service_plan_id

  # Use the storage account assigned identity for the Function Apps:
  storage_account_name          = module.storage["fnapp-${each.value.region}"].storage_account_name
  storage_account_access_key    = var.function_apps.storage_uses_managed_identity == true ? null : module.storage["fnapp-${each.value.region}"].storage_account_primary_access_key
  storage_uses_managed_identity = var.function_apps.storage_uses_managed_identity

  # Connection string for Application Insights:
  ai_connstring = data.azurerm_application_insights.ai.connection_string

  #To enable health checks for function apps
  health_check_path = var.function_apps.health_check_path

  #To enable app service log for function apps
  app_service_logs_retention_period_days = var.function_apps.app_service_logs_retention_period_days
  app_service_logs_disk_quota_mb         = var.function_apps.app_service_logs_disk_quota_mb

  # Use the ACR assigned identity for the Function Apps:
  cont_registry_use_mi = var.function_apps.cont_registry_use_mi

  # Other Function App configuration settings:
  always_on    = var.function_apps.always_on
  worker_32bit = var.function_apps.worker_32bit

  acr_mi_client_id = data.azurerm_user_assigned_identity.acr_mi.client_id
  acr_login_server = data.azurerm_container_registry.acr.login_server

  # Use the ACR assigned identity for the Function Apps too:
  # assigned_identity_ids = var.function_apps.cont_registry_use_mi ? [data.azurerm_user_assigned_identity.acr_mi.id] : []

  # use the new global RBACs
  assigned_identity_ids = local.assigned_identity_ids

  image_tag  = var.function_apps.docker_env_tag != "" ? var.function_apps.docker_env_tag : var.docker_image_tag
  image_name = "${var.function_apps.docker_img_prefix}-${lower(each.value.name_suffix)}"

  # Private Endpoint Configuration if enabled
  private_endpoint_properties = var.features.private_endpoints_enabled ? {
    private_dns_zone_ids                 = [data.terraform_remote_state.hub.outputs.private_dns_zones["${each.value.region}-app_services"].id]
    private_endpoint_enabled             = var.features.private_endpoints_enabled
    private_endpoint_subnet_id           = module.subnets["${module.regions_config[each.value.region].names.subnet}-pep"].id
    private_endpoint_resource_group_name = azurerm_resource_group.rg_private_endpoints[each.value.region].name
    private_service_connection_is_manual = var.features.private_service_connection_is_manual
  } : null

  function_app_slots = var.function_app_slots

  tags = var.tags

  depends_on = [
    module.azure_service_bus
  ]
}


# Use the merged map in your resources
resource "azurerm_role_assignment" "function_send_to_topic" {
  for_each = local.unified_service_bus_object_map

  principal_id         = module.functionapp["${each.value.function_key}-${each.value.service_bus_value.region}"].function_app_sami_id
  role_definition_name = "Azure Service Bus Data Sender"
  scope                = module.azure_service_bus[each.value.service_bus_key].namespace_id
}

locals {
  # Filter fa_config to only include those with service_bus_connections
  service_bus_function_app_map = {
    for function_key, function_values in var.function_apps.fa_config :
    function_key => function_values
    if contains(keys(function_values), "service_bus_connections")
    && (function_values.service_bus_connections != null ? length(function_values.service_bus_connections) > 0 : false)
  }

  # There are multiple maps
  # We cannot nest for loops inside a map, so first iterate all permutations as a list of objects...
  unified_service_bus_object_list = flatten([
    for service_bus_key, service_bus_value in local.service_bus_map : [
      for function_key, function_values in local.service_bus_function_app_map : merge({
        service_bus_key   = service_bus_key # 1st iterator
        function_key      = function_key    # 2nd iterator
        service_bus_value = service_bus_value
      }, function_values) # the block of key/value pairs for a specific collection
    ]
  ])

  # ...then project them into a map with unique keys (combining the iterators), for consumption by a for_each meta argument
  unified_service_bus_object_map = {
    for item in local.unified_service_bus_object_list :
    "${item.service_bus_key}-${item.function_key}" => item
  }

  assigned_identity_ids = compact(
    concat(
      [try(module.global_cohort_identity.global_mi_id, "")],
      try(var.function_apps.cont_registry_use_mi, false) ? [try(data.azurerm_user_assigned_identity.acr_mi.id, "")] : []
      # Add other IDs if required...
    )
  )

  app_settings_common = {
    DOCKER_ENABLE_CI                    = var.function_apps.docker_CI_enable
    FUNCTION_WORKER_RUNTIME             = "dotnet-isolated"
    REMOTE_DEBUGGING_ENABLED            = var.function_apps.remote_debugging_enabled
    WEBSITES_ENABLE_APP_SERVICE_STORAGE = var.function_apps.enable_appsrv_storage
    WEBSITE_PULL_IMAGE_OVER_VNET        = var.function_apps.pull_image_over_vnet
  }

  # There are multiple Function Apps and possibly multiple regions.
  # We cannot nest for loops inside a map, so first iterate all permutations of both as a list of objects...
  function_app_config_object_list = flatten([
    for region in keys(var.regions) : [
      for function, config in var.function_apps.fa_config : merge(
        {
          region   = region   # 1st iterator
          function = function # 2nd iterator
        },
        config, # the rest of the key/value pairs for a specific function
        {
          app_settings = merge(
            local.app_settings_common,
            config.env_vars_static,

            # Dynamic references to other Function App URLs
            {
              for obj in config.app_urls : obj.env_var_name => format(
                "https://%s-%s.azurewebsites.net/api/%s",
                module.regions_config[region].names["function-app"],
                var.function_apps.fa_config[obj.function_app_key].name_suffix,
                length(obj.endpoint_name) > 0 ? obj.endpoint_name : var.function_apps.fa_config[obj.function_app_key].function_endpoint_name
              )
            },

            # Dynamic reference to Key Vault
            length(config.key_vault_url) > 0 ? {
              (config.key_vault_url) = module.key_vault[region].key_vault_url
            } : {},

            # Storage - The C# code should be updated to use System Managed Identity, rather than connection string
            length(config.storage_account_env_var_name) > 0 ? merge(
              {
                (config.storage_account_env_var_name) = module.storage["file_exceptions-${region}"].storage_account_primary_connection_string
              },
              var.features.private_endpoints_enabled ? {
                "${config.storage_account_env_var_name}__blobServiceUri"  = "https://${module.storage["file_exceptions-${region}"].storage_account_name}.blob.core.windows.net"
                "${config.storage_account_env_var_name}__queueServiceUri" = "https://${module.storage["file_exceptions-${region}"].storage_account_name}.queue.core.windows.net"
              } : {}
            ) : {},

            length(config.storage_containers) > 0 ? {
              for k, v in config.storage_containers :
              v.env_var_name => v.container_name
            } : {},

            # Database connection string
            length(config.db_connection_string) > 0 ? {
              (config.db_connection_string) = "Server=${module.regions_config[region].names.sql-server}.database.windows.net; Authentication=Active Directory Managed Identity; Database=${var.sqlserver.dbs.cohman.db_name_suffix}; ApplicationIntent=ReadWrite; Pooling=true; Connection Timeout=30; Max Pool Size=300;"
            } : {}
          )
        }
      )
    ]
  ])

  # ...then project the list of objects into a map with unique keys (combining the iterators), for consumption by a for_each meta argument
  function_app_map = {
    for object in local.function_app_config_object_list : "${object.function}-${object.region}" => object
  }
}
