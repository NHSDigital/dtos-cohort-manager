module "functionapp" {
  source = ".//modules/function-app"

  names = module.config.names

  function_app        = var.function_app
  resource_group_name = module.baseline.resource_group_names[var.function_app.resource_group_index]
  location            = module.baseline.resource_group_locations[var.function_app.resource_group_index]

  asp_id     = module.app-plan.app_service_plan_id
  sa_name    = module.storage.storage_account_name
  sa_prm_key = module.storage.storage_account_primary_access_key

  tags = var.tags

}
