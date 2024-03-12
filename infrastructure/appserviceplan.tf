module "app-plan" {
  source = ".//modules/arm-app-service-plan"

  depends_on = [
    module.baseline
  ]

  names               = module.config.names
  resource_group_name = module.baseline.resource_groups[var.app_service_plan.resource_group_index].name
  location            = module.baseline.resource_groups[var.app_service_plan.resource_group_index].location

  os_type  = var.app_service_plan.os_type
  sku_name = var.app_service_plan.sku_name

  tags = var.tags

}
