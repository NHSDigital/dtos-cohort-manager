module "linux_web_app" {
  for_each = local.linux_web_app_map

  source = "../../../dtos-devops-templates/infrastructure/modules/linux-web-app"

  linux_web_app_name  = "${module.regions_config[each.value.region].names.linux-web-app}-${lower(each.value.name_suffix)}"
  resource_group_name = azurerm_resource_group.core[each.value.region].name
  location            = each.value.region

  app_settings = each.value.app_settings

  log_analytics_workspace_id                            = data.terraform_remote_state.audit.outputs.log_analytics_workspace_id[local.primary_region]
  monitor_diagnostic_setting_linux_web_app_enabled_logs = local.monitor_diagnostic_setting_linux_web_app_enabled_logs
  monitor_diagnostic_setting_linux_web_app_metrics      = local.monitor_diagnostic_setting_linux_web_app_metrics

  public_network_access_enabled = var.features.public_network_access_enabled
  vnet_integration_subnet_id    = module.subnets["${module.regions_config[each.value.region].names.subnet}-apps"].id

  rbac_role_assignments = each.value.rbac_role_assignments

  asp_id = module.app-service-plan["${each.value.app_service_plan_key}-${each.value.region}"].app_service_plan_id

  # Use the storage account assigned identity for the Linux Web Apps:
  storage_account_name       = module.storage["webapp-${each.value.region}"].storage_account_name
  storage_account_access_key = module.storage["webapp-${each.value.region}"].storage_account_primary_access_key
  storage_name               = var.linux_web_app.storage_name
  storage_type               = var.linux_web_app.storage_type
  share_name                 = var.linux_web_app.share_name

  #To enable health checks for linux web apps
  health_check_path = var.linux_web_app.health_check_path

  # Use the ACR assigned identity for the Linux Web Apps:
  cont_registry_use_mi = var.linux_web_app.cont_registry_use_mi

  # Other Linux Web App configuration settings:
  always_on    = var.linux_web_app.always_on
  worker_32bit = var.linux_web_app.worker_32bit

  acr_mi_client_id = data.azurerm_user_assigned_identity.acr_mi.client_id
  acr_login_server = data.azurerm_container_registry.acr.login_server

  # Use the ACR assigned identity for the Linux Web Apps too:
  assigned_identity_ids = var.linux_web_app.cont_registry_use_mi ? [data.azurerm_user_assigned_identity.acr_mi.id] : []

  docker_image_name = "${var.linux_web_app.docker_img_prefix}-${lower(each.value.name_suffix)}"

  # Private Endpoint Configuration if enabled
  private_endpoint_properties = var.features.private_endpoints_enabled ? {
    private_dns_zone_ids                 = [data.terraform_remote_state.hub.outputs.private_dns_zones["${each.value.region}-app_services"].id]
    private_endpoint_enabled             = var.features.private_endpoints_enabled
    private_endpoint_subnet_id           = module.subnets["${module.regions_config[each.value.region].names.subnet}-pep"].id
    private_endpoint_resource_group_name = azurerm_resource_group.rg_private_endpoints[each.value.region].name
    private_service_connection_is_manual = var.features.private_service_connection_is_manual
  } : null

  # linux_web_app_slots = var.linux_web_app_slots

  tags = var.tags
}

locals {
  app_settings_common_web_app = {
    DOCKER_ENABLE_CI                    = var.linux_web_app.docker_CI_enable
    FUNCTION_WORKER_RUNTIME             = "dotnet"
    REMOTE_DEBUGGING_ENABLED            = var.linux_web_app.remote_debugging_enabled
    WEBSITES_ENABLE_APP_SERVICE_STORAGE = var.linux_web_app.enable_appsrv_storage
    WEBSITE_PULL_IMAGE_OVER_VNET        = var.linux_web_app.pull_image_over_vnet
  }

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
            config.env_vars_static,

            # # Dynamic env vars which cannot be stored in tfvars file
            # linux_web_app == "example-linux_web_app" ? {
            #   EXAMPLE_API_KEY = data.azurerm_key_vault_secret.example[region].versionless_id
            # } : {},

            # Dynamic references to other Linux Web App URLs
            {
              for obj in config.app_urls : obj.env_var_name => format(
                "https://%s-%s.azurewebsites.net/api/%s",
                module.regions_config[region].names["linux-web-app"],
                var.linux_web_app.linux_web_app[obj.linux_web_app_key].name_suffix,
                var.linux_web_app.linux_web_app[obj.linux_web_app_key].linux_web_app_endpoint_name
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
              (config.db_connection_string) = "Server=${module.regions_config[region].names.sql-server}.database.windows.net; Authentication=Active Directory Managed Identity; Database=${var.sqlserver.dbs.cohman.db_name_suffix}"
            } : {}
          )

          # These RBAC assignments are for the Linux Web Apps only
          rbac_role_assignments = flatten([

            # Key Vault
            var.key_vault != {} && length(config.key_vault_url) > 0 ? [
              for role in local.rbac_roles_key_vault : {
                role_definition_name = role
                scope                = module.key_vault[region].key_vault_id
              }
            ] : [],

            # Storage Accounts
            [
              for account in keys(var.storage_accounts) : [
                for role in local.rbac_roles_storage : {
                  role_definition_name = role
                  scope                = module.storage["${account}-${region}"].storage_account_id
                }
              ]
            ],

            # Database
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
