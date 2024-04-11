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
  worker_32bit  = var.function_app.worker_32bit

  tags = var.tags

}
