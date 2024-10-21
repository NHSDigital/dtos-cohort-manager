module "functionapp" {
  for_each = { for pair in flatten([
    for region_key, region_functions in local.function_apps_config : [
      for function_key, function_config in region_functions : {
        region_key      = region_key
        function_key    = function_key
        function_config = function_config
      }
    ]
  ]) : "${pair.function_key}-${pair.region_key}" => pair }

  source = "git::https://github.com/NHSDigital/dtos-devops-templates.git//infrastructure/modules/function-app?ref=fa87791b4a7e8ec145c3c85926765e0d5160db29"

  function_app_name   = "${module.regions_config[each.value.region_key].names.function-app}-${lower(each.value.function_config.name_suffix)}"
  resource_group_name = module.baseline.resource_group_names[var.function_apps.resource_group_key]
  location            = module.baseline.resource_group_locations[var.function_apps.resource_group_key]

  app_settings = local.app_settings[each.value.region_key][each.value.function_key]

  asp_id     = module.app-plan.app_service_plan_id
  sa_name    = module.storage.storage_account_names["fnapp"]
  sa_prm_key = module.storage.storage_account_primary_access_keys["fnapp"]

  ai_connstring        = data.azurerm_application_insights.ai.connection_string
  worker_32bit         = var.function_apps.worker_32bit
  cont_registry_use_mi = var.function_apps.cont_registry_use_mi

  acr_mi_client_id = data.azurerm_user_assigned_identity.acr_mi.client_id
  acr_login_server = data.azurerm_container_registry.acr.login_server

  # Use the ACR assigned identity for the Function Apps too:
  assigned_identity_ids = var.function_apps.cont_registry_use_mi ? [data.azurerm_user_assigned_identity.acr_mi.id] : []

  image_tag  = var.function_apps.docker_env_tag
  image_name = "${var.function_apps.docker_img_prefix}-${lower(each.value.function_config.name_suffix)}"
}


/* --------------------------------------------------------------------------------------------------
  Data lookups used to create the Function Apps
-------------------------------------------------------------------------------------------------- */
data "azurerm_container_registry" "acr" {
  provider = azurerm.acr_subscription

  name                = var.function_apps.acr_name
  resource_group_name = var.function_apps.acr_rg_name
}

data "azurerm_user_assigned_identity" "acr_mi" {
  provider = azurerm.acr_subscription

  name                = var.function_apps.acr_mi_name
  resource_group_name = var.function_apps.acr_rg_name
}

data "azurerm_application_insights" "ai" {
  provider = azurerm.audit_subscription

  name                = var.function_apps.app_insights_name
  resource_group_name = var.function_apps.app_insights_rg_name
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
  global_app_settings = {
    DOCKER_ENABLE_CI                    = var.function_apps.docker_CI_enable
    REMOTE_DEBUGGING_ENABLED            = var.function_apps.remote_debugging_enabled
    WEBSITES_ENABLE_APP_SERVICE_STORAGE = var.function_apps.enable_appsrv_storage
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

  # Create a map of the storage accounts for each function app as defined in the storage_account_env_var_name attribute
  env_vars_storage_accounts = {
    for key, value in var.function_apps.fa_config :
    key => length(value.storage_account_env_var_name) > 0 ? {
      "${value.storage_account_env_var_name}" = module.storage.storage_account_primary_connection_strings["file_exceptions"]
    } : null
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

  # Merge the local maps into a single map taking care to remove any null values and to loop round each region and each function app where necessary:
  app_settings = {
    for region_key, region_value in module.regions_config :
    region_key => {
      for app_key, app_value in var.function_apps.fa_config :
      app_key => merge(
        local.global_app_settings,
        local.env_vars_app_urls[region_key][app_key],
        local.env_vars_storage_accounts[app_key],
        local.env_vars_storage_containers[app_key],
        local.env_vars_database_connection_strings[region_key][app_key]
      )
    }
  }
}
