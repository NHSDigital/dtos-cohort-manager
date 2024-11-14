locals {
  # Create a flat list of projects with region keys for consumption in a for_each meta argument
  app_service_plans_flatlist = flatten([
    for region_key, region_val in var.regions : [
      for asp_key, asp_val in var.app_service_plan.instances : {
        key        = "${asp_key}-${region_key}"
        asp_key    = asp_key
        asp_val    = asp_val
        region_key = region_key
      }
    ]
  ])

  # Project the above list into a map with unique keys for consumption in a for_each meta argument
  app_service_plans_map = { for asp in local.app_service_plans_flatlist : asp.key => asp }
}

module "app-service-plan" {
  for_each = local.app_service_plans_map

  source = "../../../dtos-devops-templates/infrastructure/modules/app-service-plan"

  name                = "${module.regions_config[each.value.region_key].names.app-service-plan}-${lower(each.value.asp_key)}"
  resource_group_name = azurerm_resource_group.core[each.value.region_key].name
  location            = each.value.region_key

  os_type  = var.app_service_plan.os_type
  sku_name = var.app_service_plan.sku_name

  vnet_integration_subnet_id = module.subnets["${module.regions_config[each.value.region_key].names.subnet}-apps"].id

  tags = var.tags

  ## autoscale rule
  metric = each.value.asp_val.autoscale_override != null ? coalesce(each.value.asp_val.autoscale_override.memory_percentage.metric, var.app_service_plan.autoscale.memory_percentage.metric) : var.app_service_plan.autoscale.memory_percentage.metric

  capacity_min = each.value.asp_val.autoscale_override != null ? coalesce(each.value.asp_val.autoscale_override.memory_percentage.capacity_min, var.app_service_plan.autoscale.memory_percentage.capacity_min) : var.app_service_plan.autoscale.memory_percentage.capacity_min
  capacity_max = each.value.asp_val.autoscale_override != null ? coalesce(each.value.asp_val.autoscale_override.memory_percentage.capacity_max, var.app_service_plan.autoscale.memory_percentage.capacity_max) : var.app_service_plan.autoscale.memory_percentage.capacity_max
  capacity_def = each.value.asp_val.autoscale_override != null ? coalesce(each.value.asp_val.autoscale_override.memory_percentage.capacity_def, var.app_service_plan.autoscale.memory_percentage.capacity_def) : var.app_service_plan.autoscale.memory_percentage.capacity_def

  time_grain       = each.value.asp_val.autoscale_override != null ? coalesce(each.value.asp_val.autoscale_override.memory_percentage.time_grain, var.app_service_plan.autoscale.memory_percentage.time_grain) : var.app_service_plan.autoscale.memory_percentage.time_grain
  statistic        = each.value.asp_val.autoscale_override != null ? coalesce(each.value.asp_val.autoscale_override.memory_percentage.statistic, var.app_service_plan.autoscale.memory_percentage.statistic) : var.app_service_plan.autoscale.memory_percentage.statistic
  time_window      = each.value.asp_val.autoscale_override != null ? coalesce(each.value.asp_val.autoscale_override.memory_percentage.time_window, var.app_service_plan.autoscale.memory_percentage.time_window) : var.app_service_plan.autoscale.memory_percentage.time_window
  time_aggregation = each.value.asp_val.autoscale_override != null ? coalesce(each.value.asp_val.autoscale_override.memory_percentage.time_aggregation, var.app_service_plan.autoscale.memory_percentage.time_aggregation) : var.app_service_plan.autoscale.memory_percentage.time_aggregation

  inc_operator        = each.value.asp_val.autoscale_override != null ? coalesce(each.value.asp_val.autoscale_override.memory_percentage.inc_operator, var.app_service_plan.autoscale.memory_percentage.inc_operator) : var.app_service_plan.autoscale.memory_percentage.inc_operator
  inc_threshold       = each.value.asp_val.autoscale_override != null ? coalesce(each.value.asp_val.autoscale_override.memory_percentage.inc_threshold, var.app_service_plan.autoscale.memory_percentage.inc_threshold) : var.app_service_plan.autoscale.memory_percentage.inc_threshold
  inc_scale_direction = each.value.asp_val.autoscale_override != null ? coalesce(each.value.asp_val.autoscale_override.memory_percentage.inc_scale_direction, var.app_service_plan.autoscale.memory_percentage.inc_scale_direction) : var.app_service_plan.autoscale.memory_percentage.inc_scale_direction
  inc_scale_type      = each.value.asp_val.autoscale_override != null ? coalesce(each.value.asp_val.autoscale_override.memory_percentage.inc_scale_type, var.app_service_plan.autoscale.memory_percentage.inc_scale_type) : var.app_service_plan.autoscale.memory_percentage.inc_scale_type
  inc_scale_value     = each.value.asp_val.autoscale_override != null ? coalesce(each.value.asp_val.autoscale_override.memory_percentage.inc_scale_value, var.app_service_plan.autoscale.memory_percentage.inc_scale_value) : var.app_service_plan.autoscale.memory_percentage.inc_scale_value
  inc_scale_cooldown  = each.value.asp_val.autoscale_override != null ? coalesce(each.value.asp_val.autoscale_override.memory_percentage.inc_scale_cooldown, var.app_service_plan.autoscale.memory_percentage.inc_scale_cooldown) : var.app_service_plan.autoscale.memory_percentage.inc_scale_cooldown

  dec_operator        = each.value.asp_val.autoscale_override != null ? coalesce(each.value.asp_val.autoscale_override.memory_percentage.dec_operator, var.app_service_plan.autoscale.memory_percentage.dec_operator) : var.app_service_plan.autoscale.memory_percentage.dec_operator
  dec_threshold       = each.value.asp_val.autoscale_override != null ? coalesce(each.value.asp_val.autoscale_override.memory_percentage.dec_threshold, var.app_service_plan.autoscale.memory_percentage.dec_threshold) : var.app_service_plan.autoscale.memory_percentage.dec_threshold
  dec_scale_direction = each.value.asp_val.autoscale_override != null ? coalesce(each.value.asp_val.autoscale_override.memory_percentage.dec_scale_direction, var.app_service_plan.autoscale.memory_percentage.dec_scale_direction) : var.app_service_plan.autoscale.memory_percentage.dec_scale_direction
  dec_scale_type      = each.value.asp_val.autoscale_override != null ? coalesce(each.value.asp_val.autoscale_override.memory_percentage.dec_scale_type, var.app_service_plan.autoscale.memory_percentage.dec_scale_type) : var.app_service_plan.autoscale.memory_percentage.dec_scale_type
  dec_scale_value     = each.value.asp_val.autoscale_override != null ? coalesce(each.value.asp_val.autoscale_override.memory_percentage.dec_scale_value, var.app_service_plan.autoscale.memory_percentage.dec_scale_value) : var.app_service_plan.autoscale.memory_percentage.dec_scale_value
  dec_scale_cooldown  = each.value.asp_val.autoscale_override != null ? coalesce(each.value.asp_val.autoscale_override.memory_percentage.dec_scale_cooldown, var.app_service_plan.autoscale.memory_percentage.dec_scale_cooldown) : var.app_service_plan.autoscale.memory_percentage.dec_scale_cooldown
}
