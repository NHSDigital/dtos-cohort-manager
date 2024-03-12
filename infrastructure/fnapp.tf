module "functionapp" {
  source = ".//modules/arm-azure-function-app"

  depends_on = [
    module.storage
  ]

  fnapp_count         = var.function_app.fnapp_count
  name                = module.config.names.function-app
  resource_group_name = module.baseline.resource_groups[var.function_app.resource_group_index].name
  location            = module.baseline.resource_groups[var.function_app.resource_group_index].location

  appsvcplan_name = module.config.names.app-service-plan
  sa_name         = "${module.config.names.storage-account}${var.storage_accounts.fnapp.name_suffix}"


  tags = var.tags

}
