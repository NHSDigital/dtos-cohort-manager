module "app-plan" {
  source = ".//modules/app-service-plan"

  names               = module.config.names
  resource_group_name = module.baseline.resource_group_names[var.app_service_plan.resource_group_key]
  location            = module.baseline.resource_group_locations[var.app_service_plan.resource_group_key]

  os_type  = var.app_service_plan.os_type
  sku_name = var.app_service_plan.sku_name

  tags = var.tags

}
