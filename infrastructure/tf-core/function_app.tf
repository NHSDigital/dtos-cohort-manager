module "functionapp" {
  for_each = local.function_app_map

  source = "../../../dtos-devops-templates/infrastructure/modules/function-app"

  function_app_name   = "${module.regions_config[each.value.region_key].names.function-app}-${lower(each.value.function_config.name_suffix)}"
  resource_group_name = azurerm_resource_group.core[each.value.region_key].name
  location            = each.value.region_key

  app_settings = local.app_settings[each.value.region_key][each.value.function_key]

  log_analytics_workspace_id                           = data.terraform_remote_state.audit.outputs.log_analytics_workspace_id[local.primary_region]
  monitor_diagnostic_setting_function_app_enabled_logs = ["FunctionAppLogs"]
  monitor_diagnostic_setting_function_app_metrics      = ["AllMetrics"]

  public_network_access_enabled = var.features.public_network_access_enabled
  vnet_integration_subnet_id    = module.subnets["${module.regions_config[each.value.region_key].names.subnet}-apps"].id

  rbac_role_assignments = local.rbac_role_assignments[each.value.region_key]

  asp_id = module.app-service-plan["${each.value.function_config.app_service_plan_key}-${each.value.region_key}"].app_service_plan_id

  # Use the storage account assigned identity for the Function Apps:
  storage_account_name          = module.storage["fnapp-${each.value.region_key}"].storage_account_name
  storage_account_access_key    = var.function_apps.storage_uses_managed_identity == true ? null : module.storage["fnapp-${each.value.region_key}"].storage_account_primary_access_key
  storage_uses_managed_identity = var.function_apps.storage_uses_managed_identity

  # Connection string for Application Insights:
  ai_connstring = data.azurerm_application_insights.ai.connection_string

  # Use the ACR assigned identity for the Function Apps:
  cont_registry_use_mi = var.function_apps.cont_registry_use_mi

  # Other Function App configuration settings:
  always_on    = var.function_apps.always_on
  worker_32bit = var.function_apps.worker_32bit

  acr_mi_client_id = data.azurerm_user_assigned_identity.acr_mi.client_id
  acr_login_server = data.azurerm_container_registry.acr.login_server

  # Use the ACR assigned identity for the Function Apps too:
  assigned_identity_ids = var.function_apps.cont_registry_use_mi ? [data.azurerm_user_assigned_identity.acr_mi.id] : []

  image_tag  = var.function_apps.docker_env_tag
  image_name = "${var.function_apps.docker_img_prefix}-${lower(each.value.function_config.name_suffix)}"

  # Private Endpoint Configuration if enabled
  private_endpoint_properties = var.features.private_endpoints_enabled ? {
    private_dns_zone_ids                 = [data.terraform_remote_state.hub.outputs.private_dns_zone_app_services[each.value.region_key].private_dns_zone.id]
    private_endpoint_enabled             = var.features.private_endpoints_enabled
    private_endpoint_subnet_id           = module.subnets["${module.regions_config[each.value.region_key].names.subnet}-pep"].id
    private_endpoint_resource_group_name = azurerm_resource_group.rg_private_endpoints[each.value.region_key].name
    private_service_connection_is_manual = var.features.private_service_connection_is_manual
  } : null

  function_app_slots = var.function_app_slots

  tags = var.tags
}


/* --------------------------------------------------------------------------------------------------
  Function App Access Policies
-------------------------------------------------------------------------------------------------- */

# Loop through the Key Vault URLs for each region and create the Key Vault Access Policies for each Function App:
resource "azurerm_key_vault_access_policy" "functionapp" {
  for_each = local.keyvault_function_app_object_ids_map

  key_vault_id = each.value.key_vault_id
  object_id    = each.value.function_app_sami_id
  tenant_id    = data.azurerm_client_config.current.tenant_id

  secret_permissions = [
    "Get",
    "List"
  ]

  certificate_permissions = [
    "Get",
    "List"
  ]
}

/* --------------------------------------------------------------------------------------------------
  RBAC roles to assign to the Function Apps
-------------------------------------------------------------------------------------------------- */
locals {
  rbac_role_assignments = {
    for region_key in keys(module.regions_config) :
    region_key => concat(
      [
        for _, role_value in local.rbac_roles_storage : {
          role_definition_name = role_value
          scope                = module.storage["fnapp-${region_key}"].storage_account_id
        }
      ],
      [
        for _, role_value in local.rbac_roles_storage : {
          role_definition_name = role_value
          scope                = module.storage["file_exceptions-${region_key}"].storage_account_id
        }
      ],
      [
        for _, role_value in local.rbac_roles_database : {
          role_definition_name = role_value
          scope                = module.azure_sql_server[region_key].sql_server_id
        }
      ]
    )
  }
}

/* --------------------------------------------------------------------------------------------------
  Local variables used to create the Environment Variables for the Function Apps
-------------------------------------------------------------------------------------------------- */
locals {

  # Create a map of the function apps config per region
  function_apps_config = {
    for region_key, region_value in module.regions_config :
    region_key => {
      for key, value in var.function_apps.fa_config :
      key => value
    }
  }


  # To Do - move these directly into the tfvars file as a map as this way limits adding extra values
  # WEBSITE_PULL_IMAGE_OVER_VNET reuses the private_endpoints_enabled variable as these settings are implicitly coupled.
  global_app_settings = {
    DOCKER_ENABLE_CI                    = var.function_apps.docker_CI_enable
    REMOTE_DEBUGGING_ENABLED            = var.function_apps.remote_debugging_enabled
    WEBSITES_ENABLE_APP_SERVICE_STORAGE = var.function_apps.enable_appsrv_storage
    WEBSITE_PULL_IMAGE_OVER_VNET        = var.features.private_endpoints_enabled
  }

  # Create a map of the function app urls for each function app
  env_vars_app_urls = {
    for region_key, region_value in module.regions_config :
    region_key => {
      for key, value in var.function_apps.fa_config :
      key => {
        for app_url_key, app_url_value in value.app_urls :
        app_url_value.env_var_name => "https://${module.regions_config[region_key].names.function-app}-${var.function_apps.fa_config[app_url_value.function_app_key].name_suffix}.azurewebsites.net/api/${var.function_apps.fa_config[app_url_value.function_app_key].function_endpoint_name}"
      }
    }
  }

  # Create a map of the static environment variables for each function app
  env_vars_static = {
    for region_key, region_value in module.regions_config :
    region_key => {
      for key, value in var.function_apps.fa_config :
      key => {
        for env_var_key, env_var_value in value.env_vars_static :
        env_var_value.env_var_name => env_var_value.env_var_value
      }
    }
  }

  # Create a map of the storage accounts for each function app as defined in the storage_account_env_var_name attribute
  # Should not need the following entry if we are using managed identity for storage access, but the C# code is not yet
  # ready to support this so we are using the storage account key for now.
  env_vars_storage_accounts = {
    for region_key, region_value in module.regions_config :
    region_key => {
      for key, value in var.function_apps.fa_config :
      key => length(value.storage_account_env_var_name) > 0 ? {
        "${value.storage_account_env_var_name}" = module.storage["file_exceptions-${region_key}"].storage_account_primary_connection_string
      } : null
    }
  }

  env_vars_storage_accounts_private_blob = {
    for region_key, region_value in module.regions_config :
    region_key => {
      for key, value in var.function_apps.fa_config :
      key => length(value.storage_account_env_var_name) > 0 ? {
        "${value.storage_account_env_var_name}__blobServiceUri" = "https://${module.storage["file_exceptions-${region_key}"].storage_account_name}.blob.core.windows.net"
      } : null
    }
    if var.features.private_endpoints_enabled == true
  }

  env_vars_storage_accounts_private_queue = {
    for region_key, region_value in module.regions_config :
    region_key => {
      for key, value in var.function_apps.fa_config :
      key => length(value.storage_account_env_var_name) > 0 ? {
        "${value.storage_account_env_var_name}__queueServiceUri" = "https://${module.storage["file_exceptions-${region_key}"].storage_account_name}.queue.core.windows.net"
      } : null
    }
    if var.features.private_endpoints_enabled == true
  }

  # Create a map of the storage containers for each function app as defined in the storage_containers attribute
  env_vars_storage_containers = {
    for key, value in var.function_apps.fa_config :
    key => length(value.storage_containers) > 0 ? {
      for container_key, container_value in value.storage_containers :
      container_value.env_var_name => container_value.container_name
    } : null
  }

  # Create a map of the database connection strings for each function app that requires one
  env_vars_database_connection_strings = {
    for region_key, region_value in module.regions_config :
    region_key => {
      for key, value in var.function_apps.fa_config :
      key => length(value.db_connection_string) > 0 ? {
      "${value.db_connection_string}" = "Server=${module.regions_config[region_key].names.sql-server}.database.windows.net; Authentication=Active Directory Managed Identity; Database=${var.sqlserver.dbs.cohman.db_name_suffix}" }
      : null
    }
  }

  # Create a map of the key vault urls for each function app that requires one
  env_vars_key_vault_urls = {
    for region_key, region_value in module.regions_config :
    region_key => {
      for key, value in var.function_apps.fa_config :
      key => length(value.key_vault_url) > 0 ? {
      "${value.key_vault_url}" = module.key_vault[region_key].key_vault_url }
      : null
    }
  }

  # Merge the local maps into a single map taking care to remove any null values and to loop round each region and each function app where necessary:
  app_settings = {
    for region_key, region_value in module.regions_config :
    region_key => {
      for app_key, app_value in var.function_apps.fa_config :
      app_key => merge(
        local.global_app_settings,
        try(local.env_vars_app_urls[region_key][app_key], {}),
        try(local.env_vars_static[region_key][app_key], {}),
        try(local.env_vars_storage_accounts[region_key][app_key], {}),
        try(local.env_vars_storage_accounts_private_blob[region_key][app_key], {}),
        try(local.env_vars_storage_accounts_private_queue[region_key][app_key], {}),
        try(local.env_vars_storage_containers[app_key], {}),
        try(local.env_vars_database_connection_strings[region_key][app_key], {}),
        try(local.env_vars_key_vault_urls[region_key][app_key], {})
      )
    }
  }

  # Finaly build a "super map" of all the app settings for each function app in each region
  function_app_map = {
    for value in flatten([
      for region_key, region_functions in local.function_apps_config : [
        for function_key, function_config in region_functions : {
          region_key      = region_key
          function_key    = function_key
          function_config = function_config
        }
      ]
    ]) : "${value.function_key}-${value.region_key}" => value
  }

  # Create a flat list for the key vault access policy resource contianing just the details
  # for functions that have key vault urls
  keyvault_function_app_object_ids = flatten([
    for region_key, region_value in module.regions_config :
    [
      for function_key, function_value in local.env_vars_key_vault_urls[region_key] :
      {
        region_key           = region_key
        function_key         = function_key
        key_vault_id         = module.key_vault[region_key].key_vault_id
        function_app_sami_id = module.functionapp["${function_key}-${region_key}"].function_app_sami_id
      }
      if function_value != null
    ]
  ])
  # Project the above list into a map with unique keys for consumption in a for_each meta argument
  # (although in this case we don't actually need the key as we will create everything from the list as-is)
  keyvault_function_app_object_ids_map = {
    for value in local.keyvault_function_app_object_ids : "${value.function_key}-${value.region_key}" => value
  }
}
