module "functionapp" {
  source = ".//modules/function-app"

  names = module.config.names

  function_app        = var.function_app.fa_config
  resource_group_name = module.baseline.resource_group_names[var.function_app.resource_group_key]
  location            = module.baseline.resource_group_locations[var.function_app.resource_group_key]

  asp_id     = module.app-plan.app_service_plan_id
  sa_name    = module.storage.storage_account_name
  sa_prm_key = module.storage.storage_account_primary_access_key

  ai_connstring = module.app_insights.ai_connection_string

  gl_worker_32bit = var.function_app.gl_worker_32bit

  app_settings = var.function_app.gl_app_settings

  acr_registry_url     = module.acr.login_server
  cont_registry_use_mi = var.function_app.gl_cont_registry_use_mi

  acr_mi_id        = module.acr.mi_id
  acr_mi_client_id = module.acr.mi_client_id

  image_tag = var.function_app.gl_docker_env_tag

  #Specific FNApp settings:
  caasfolder_STORAGE = module.storage.sa_fe_primary_connection_string

  tags = var.tags

}
