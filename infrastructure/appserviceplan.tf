module "app-plan" {
  source = ".//modules/app-service-plan"

  names               = module.config.names
  resource_group_name = module.baseline.resource_group_names[var.app_service_plan.resource_group_key]
  location            = module.baseline.resource_group_locations[var.app_service_plan.resource_group_key]

  os_type  = var.app_service_plan.os_type
  sku_name = var.app_service_plan.sku_name

  tags = var.tags

  ## autoscale rule

  metric = var.app_service_plan.autoscale.memory_percentage.metric

  capacity_min = var.app_service_plan.autoscale.memory_percentage.capacity_min
  capacity_max = var.app_service_plan.autoscale.memory_percentage.capacity_max
  capacity_def = var.app_service_plan.autoscale.memory_percentage.capacity_def

  time_grain       = var.app_service_plan.autoscale.memory_percentage.time_grain
  statistic        = var.app_service_plan.autoscale.memory_percentage.statistic
  time_window      = var.app_service_plan.autoscale.memory_percentage.time_window
  time_aggregation = var.app_service_plan.autoscale.memory_percentage.time_aggregation

  inc_operator        = var.app_service_plan.autoscale.memory_percentage.inc_operator
  inc_threshold       = var.app_service_plan.autoscale.memory_percentage.inc_threshold
  inc_scale_direction = var.app_service_plan.autoscale.memory_percentage.inc_scale_direction
  inc_scale_type      = var.app_service_plan.autoscale.memory_percentage.inc_scale_type
  inc_scale_value     = var.app_service_plan.autoscale.memory_percentage.inc_scale_value
  inc_scale_cooldown  = var.app_service_plan.autoscale.memory_percentage.inc_scale_cooldown

  dec_operator        = var.app_service_plan.autoscale.memory_percentage.dec_operator
  dec_threshold       = var.app_service_plan.autoscale.memory_percentage.dec_threshold
  dec_scale_direction = var.app_service_plan.autoscale.memory_percentage.dec_scale_direction
  dec_scale_type      = var.app_service_plan.autoscale.memory_percentage.dec_scale_type
  dec_scale_value     = var.app_service_plan.autoscale.memory_percentage.dec_scale_value
  dec_scale_cooldown  = var.app_service_plan.autoscale.memory_percentage.dec_scale_cooldown
}
