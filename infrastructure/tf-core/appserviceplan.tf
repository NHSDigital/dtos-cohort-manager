module "app-service-plan" {
  for_each = local.app_service_plans_flatlist

  source = "git::https://github.com/NHSDigital/dtos-devops-templates.git//infrastructure/modules/app-service-plan?ref=6dbb0d4f42e3fd1f94d4b8e85ef596b7d01844bc"

  name                = "${module.regions_config[each.value.region_key].names.app-service-plan}-${lower(each.value.asp_key)}"
  resource_group_name = module.baseline.resource_group_names[var.app_service_plan.resource_group_key]
  location            = module.baseline.resource_group_locations[var.app_service_plan.resource_group_key]

  os_type  = var.app_service_plan.os_type
  sku_name = var.app_service_plan.sku_name

  vnet_integration_subnet_id = module.subnets["${module.regions_config[each.value.region_key].names.subnet}-apps"].id

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

locals {

  # Create a flat list of projects with region keys for consumption in a for_each meta argument
  app_service_plans_flatlist = flatten([
    for region_key, region_val in var.regions : [
      for asp_key, asp_val in var.app_service_plan.instances : {
        key                 = "${asp_key}-${region_key}"
        asp_key             = asp_key
        asp_val             = asp_val
        region_key          = region_key
        autoscale_override  = asp_val.autoscale_override
      }
    ]
  ])

  # Project the above list into a map with unique keys for consumption in a for_each meta argument
  app_service_plans_map = { for asp in local.app_service_plans_flatlist : asp.key => asp }
}

output "app_service_plans" {
  value = local.app_service_plans_map
}
