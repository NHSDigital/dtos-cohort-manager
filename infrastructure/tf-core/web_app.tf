module "linux_web_app" {
  for_each = local.linux_web_app_map

  source = "../../../dtos-devops-templates/infrastructure/modules/linux-web-app"

  providers = {
    azurerm     = azurerm
    azurerm.dns = azurerm.hub # For Custom Domains DNS challenge records
  }

  linux_web_app_name  = "${module.regions_config[each.value.region].names.linux-web-app}-${lower(each.value.name_suffix)}"
  resource_group_name = azurerm_resource_group.core[each.value.region].name
  location            = each.value.region

  acr_login_server                                      = data.azurerm_container_registry.acr.login_server
  acr_mi_client_id                                      = data.azurerm_user_assigned_identity.acr_mi.client_id
  always_on                                             = var.linux_web_app.always_on
  app_settings                                          = each.value.app_settings
  asp_id                                                = module.app-service-plan["${each.value.app_service_plan_key}-${each.value.region}"].app_service_plan_id
  assigned_identity_ids                                 = var.linux_web_app.cont_registry_use_mi ? [data.azurerm_user_assigned_identity.acr_mi.id] : []
  cont_registry_use_mi                                  = var.linux_web_app.cont_registry_use_mi
  custom_domains                                        = each.value.custom_domains
  docker_image_name                                     = "${var.linux_web_app.docker_img_prefix}-${lower(each.value.name_suffix)}:${var.linux_web_app.docker_env_tag != "" ? var.linux_web_app.docker_env_tag : var.docker_image_tag}"
  health_check_path                                     = var.linux_web_app.health_check_path
  linux_web_app_slots                                   = var.linux_web_app_slots
  log_analytics_workspace_id                            = data.terraform_remote_state.audit.outputs.log_analytics_workspace_id[local.primary_region]
  monitor_diagnostic_setting_linux_web_app_enabled_logs = local.monitor_diagnostic_setting_linux_web_app_enabled_logs
  monitor_diagnostic_setting_linux_web_app_metrics      = local.monitor_diagnostic_setting_linux_web_app_metrics

  private_endpoint_properties = var.features.private_endpoints_enabled ? {
    private_dns_zone_ids                 = [data.terraform_remote_state.hub.outputs.private_dns_zones["${each.value.region}-app_services"].id]
    private_endpoint_enabled             = var.features.private_endpoints_enabled
    private_endpoint_resource_group_name = azurerm_resource_group.rg_private_endpoints[each.value.region].name
    private_endpoint_subnet_id           = module.subnets["${module.regions_config[each.value.region].names.subnet}-pep-dmz"].id
    private_service_connection_is_manual = var.features.private_service_connection_is_manual
  } : null

  public_dns_zone_rg_name       = data.terraform_remote_state.hub.outputs.public_dns_zone_rg_name
  public_network_access_enabled = var.features.public_network_access_enabled
  rbac_role_assignments         = each.value.rbac_role_assignments
  # share_name                 = var.linux_web_app.share_name
  # storage_account_access_key = module.storage["webapp-${each.value.region}"].storage_account_primary_access_key
  # storage_account_name       = module.storage["webapp-${each.value.region}"].storage_account_name
  # storage_name               = var.linux_web_app.storage_name
  # storage_type               = var.linux_web_app.storage_type
  vnet_integration_subnet_id = module.subnets["${module.regions_config[each.value.region].names.subnet}-webapps"].id
  wildcard_ssl_cert_id       = each.value.custom_domains != null ? module.app-service-plan["${each.value.app_service_plan_key}-${each.value.region}"].wildcard_ssl_cert_id : null
  worker_32bit               = var.linux_web_app.worker_32bit

  tags = merge(
    #var.tags,
    # These special tags are necessary to enable Application Insights since the azurerm API does not currently offer a way to do it
    {
      "hidden-link: /app-insights-resource-id"         = data.azurerm_application_insights.ai.id
      "hidden-link: /app-insights-instrumentation-key" = data.azurerm_application_insights.ai.instrumentation_key
      "hidden-link: /app-insights-conn-string"         = data.azurerm_application_insights.ai.connection_string
    }
  )
}

locals {
  app_settings_common_web_app = {
    # This whole block of key/value pairs is necessary to enable Application Insights
    APPINSIGHTS_INSTRUMENTATIONKEY                  = data.azurerm_application_insights.ai.instrumentation_key
    APPINSIGHTS_PROFILERFEATURE_VERSION             = "1.0.0"
    APPINSIGHTS_SNAPSHOTFEATURE_VERSION             = "1.0.0"
    APPLICATIONINSIGHTS_CONNECTION_STRING           = data.azurerm_application_insights.ai.connection_string
    APPLICATIONINSIGHTS_CONFIGURATION_CONTENT       = null
    ApplicationInsightsAgent_EXTENSION_VERSION      = "~3"
    DiagnosticServices_EXTENSION_VERSION            = "~3"
    InstrumentationEngine_EXTENSION_VERSION         = "disabled"
    SnapshotDebugger_EXTENSION_VERSION              = "disabled"
    XDT_MicrosoftApplicationInsights_BaseExtensions = "disabled"
    XDT_MicrosoftApplicationInsights_Mode           = "recommended"
    XDT_MicrosoftApplicationInsights_PreemptSdk     = "disabled"
    # end of Application Insights settings

    DOCKER_ENABLE_CI                    = var.linux_web_app.docker_CI_enable
    REMOTE_DEBUGGING_ENABLED            = var.linux_web_app.remote_debugging_enabled
    WEBSITES_ENABLE_APP_SERVICE_STORAGE = var.linux_web_app.enable_appsrv_storage
    WEBSITE_PULL_IMAGE_OVER_VNET        = var.linux_web_app.pull_image_over_vnet
  }

  # A sticky_settings block is necessary for the Web App's Application Insights config to closely match one created in Portal
  # Sticky settings are defined as app_settings and connection_strings which will NOT be swapped between slots during a swap operation
  # However we want the logging consistently targeted at a single Application Insights instance, so we can omit this.

  # There are multiple Linux Web Apps and possibly multiple regions.
  # We cannot nest for loops inside a map, so first iterate all permutations of both as a list of objects...
  linux_web_app_config_object_list = flatten([
    for region in keys(var.regions) : [
      for linux_web_app, config in var.linux_web_app.linux_web_app_config : merge(
        {
          region        = region        # 1st iterator
          linux_web_app = linux_web_app # 2nd iterator
        },
        config, # the rest of the key/value pairs for a specific linux_web_app
        {
          app_settings = merge(
            local.app_settings_common_web_app,
            config.env_vars.static,
            {
              for k, v in config.env_vars.from_key_vault : k => "@Microsoft.KeyVault(SecretUri=${module.key_vault[region].key_vault_url}secrets/${v})"
            },
            {
              for k, v in config.env_vars.local_urls : k => format(v, module.regions_config[region].names["function-app"]) # Function App and Web App have the same naming prefix
            },
            length(config.db_connection_string) > 0 ? {
              (config.db_connection_string) = "Server=${module.regions_config[region].names.sql-server}.database.windows.net; Authentication=Active Directory Managed Identity; Database=${var.sqlserver.dbs.cohman.db_name_suffix}"
            } : {}
          )

          # These RBAC assignments are for the Linux Web Apps only
          rbac_role_assignments = flatten([
            var.key_vault != {} && length(config.env_vars.from_key_vault) > 0 ? [
              for role in local.rbac_roles_key_vault : {
                role_definition_name = role
                scope                = module.key_vault[region].key_vault_id
              }
            ] : [],
            [
              for account in keys(var.storage_accounts) : [
                for role in local.rbac_roles_storage : {
                  role_definition_name = role
                  scope                = module.storage["${account}-${region}"].storage_account_id
                }
              ]
            ],
            [
              for role in local.rbac_roles_database : {
                role_definition_name = role
                scope                = module.azure_sql_server[region].sql_server_id
              }
            ]
          ])
        }
      )
    ]
  ])

  # ...then project the list of objects into a map with unique keys (combining the iterators), for consumption by a for_each meta argument
  linux_web_app_map = {
    for object in local.linux_web_app_config_object_list : "${object.linux_web_app}-${object.region}" => object
  }
}
