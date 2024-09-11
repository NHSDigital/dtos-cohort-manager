module "functionapp" {
  source = ".//modules/function-app"

  providers = {
    azurerm                  = azurerm
    azurerm.acr_subscription = azurerm.acr_subscription
  }

  names = module.config.names

  function_app        = var.function_app.fa_config
  resource_group_name = module.baseline.resource_group_names[var.function_app.resource_group_key]
  location            = module.baseline.resource_group_locations[var.function_app.resource_group_key]

  asp_id     = module.app-plan.app_service_plan_id
  sa_name    = module.storage.storage_account_names["fnapp"]
  sa_prm_key = module.storage.storage_account_primary_access_keys["fnapp"]

  ai_connstring        = module.app_insights.ai_connection_string_audit
  gl_worker_32bit      = var.function_app.gl_worker_32bit
  cont_registry_use_mi = var.function_app.gl_cont_registry_use_mi

  acr_name    = var.function_app.acr_name
  acr_rg_name = var.function_app.acr_rg_name
  acr_mi_name = var.function_app.acr_mi_name

  image_tag             = var.function_app.gl_docker_env_tag
  docker_img_prefix     = var.function_app.gl_docker_img_prefix
  docker_CI_enable      = var.function_app.gl_docker_CI_enable
  enable_appsrv_storage = var.function_app.gl_enable_appsrv_storage

  #Specific FNApp settings:
  caasfolder_STORAGE = module.storage.storage_account_primary_connection_strings["file_exceptions"]
  db_name            = var.sqlserver.dbs.cohman.db_name_suffix

  tags = var.tags

}
