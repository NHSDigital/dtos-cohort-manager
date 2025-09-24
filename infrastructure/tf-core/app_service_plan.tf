locals {
  # There are multiple App Service Plans and possibly multiple regions.
  # We cannot nest for loops inside a map, so first iterate all permutations of both as a list of objects...
  app_service_object_list = flatten([
    for region in keys(var.regions) : [
      for app_service_plan, config in var.app_service_plan.instances : merge(
        {
          region           = region           # 1st iterator
          app_service_plan = app_service_plan # 2nd iterator
        },
        config # the rest of the key/value pairs for a specific app_service_plan
      )
    ]
  ])

  # ...then project the list of objects into a map with unique keys (combining the iterators), for consumption by a for_each meta argument
  app_service_plans_map = {
    for object in local.app_service_object_list : "${object.app_service_plan}-${object.region}" => object
  }
}

module "app-service-plan" {
  for_each = local.app_service_plans_map

  source = "../../../dtos-devops-templates/infrastructure/modules/app-service-plan"

  name                = "${module.regions_config[each.value.region].names.app-service-plan}-${lower(each.value.app_service_plan)}"
  resource_group_name = azurerm_resource_group.core[each.value.region].name
  location            = each.value.region

  log_analytics_workspace_id                        = data.terraform_remote_state.audit.outputs.log_analytics_workspace_id[local.primary_region]
  monitor_diagnostic_setting_appserviceplan_metrics = local.monitor_diagnostic_setting_appserviceplan_metrics

  enable_monitoring              = var.features.monitoring_enabled && var.monitor_action_groups != {} && var.app_service_plan.monitor_action_group_key != null
  action_group_id                = var.monitor_action_groups != {} && var.app_service_plan.monitor_action_group_key != null ? module.monitor_action_group[var.app_service_plan.monitor_action_group_key].monitor_action_group.id : null
  resource_group_name_monitoring = var.monitor_action_groups != {} && var.app_service_plan.monitor_action_group_key != null ? azurerm_resource_group.monitoring.name : null

  os_type                                          = lookup(each.value, "os_type", var.app_service_plan.os_type)
  sku_name                                         = each.value.sku_name
  zone_balancing_enabled                           = lookup(each.value, "zone_balancing_enabled", var.app_service_plan.zone_balancing_enabled)
  vnet_integration_subnet_id                       = module.subnets["${module.regions_config[each.value.region].names.subnet}-apps"].id
  wildcard_ssl_cert_name                           = each.value.wildcard_ssl_cert_key
  wildcard_ssl_cert_pfx_blob_key_vault_secret_name = each.value.wildcard_ssl_cert_key != null ? data.terraform_remote_state.hub.outputs.certificates[each.value.wildcard_ssl_cert_key].key_vault_certificate[each.value.region].pfx_blob_secret_name : null
  wildcard_ssl_cert_pfx_password                   = each.value.wildcard_ssl_cert_key != null ? data.terraform_remote_state.hub.outputs.certificates[each.value.wildcard_ssl_cert_key].key_vault_certificate[each.value.region].pfx_password : null
  wildcard_ssl_cert_key_vault_id                   = each.value.wildcard_ssl_cert_key != null ? data.terraform_remote_state.hub.outputs.key_vault[each.value.region].key_vault_id : null

  tags = var.tags

  ## autoscale rule
  metric = each.value.autoscale_override != null ? coalesce(each.value.autoscale_override.scaling_rule.metric, var.app_service_plan.autoscale.scaling_rule.metric) : var.app_service_plan.autoscale.scaling_rule.metric

  capacity_min = each.value.autoscale_override != null ? coalesce(each.value.autoscale_override.scaling_rule.capacity_min, var.app_service_plan.autoscale.scaling_rule.capacity_min) : var.app_service_plan.autoscale.scaling_rule.capacity_min
  capacity_max = each.value.autoscale_override != null ? coalesce(each.value.autoscale_override.scaling_rule.capacity_max, var.app_service_plan.autoscale.scaling_rule.capacity_max) : var.app_service_plan.autoscale.scaling_rule.capacity_max
  capacity_def = each.value.autoscale_override != null ? coalesce(each.value.autoscale_override.scaling_rule.capacity_def, var.app_service_plan.autoscale.scaling_rule.capacity_def) : var.app_service_plan.autoscale.scaling_rule.capacity_def

  time_grain       = each.value.autoscale_override != null ? coalesce(each.value.autoscale_override.scaling_rule.time_grain, var.app_service_plan.autoscale.scaling_rule.time_grain) : var.app_service_plan.autoscale.scaling_rule.time_grain
  statistic        = each.value.autoscale_override != null ? coalesce(each.value.autoscale_override.scaling_rule.statistic, var.app_service_plan.autoscale.scaling_rule.statistic) : var.app_service_plan.autoscale.scaling_rule.statistic
  time_window      = each.value.autoscale_override != null ? coalesce(each.value.autoscale_override.scaling_rule.time_window, var.app_service_plan.autoscale.scaling_rule.time_window) : var.app_service_plan.autoscale.scaling_rule.time_window
  time_aggregation = each.value.autoscale_override != null ? coalesce(each.value.autoscale_override.scaling_rule.time_aggregation, var.app_service_plan.autoscale.scaling_rule.time_aggregation) : var.app_service_plan.autoscale.scaling_rule.time_aggregation

  inc_operator        = each.value.autoscale_override != null ? coalesce(each.value.autoscale_override.scaling_rule.inc_operator, var.app_service_plan.autoscale.scaling_rule.inc_operator) : var.app_service_plan.autoscale.scaling_rule.inc_operator
  inc_threshold       = each.value.autoscale_override != null ? coalesce(each.value.autoscale_override.scaling_rule.inc_threshold, var.app_service_plan.autoscale.scaling_rule.inc_threshold) : var.app_service_plan.autoscale.scaling_rule.inc_threshold
  inc_scale_direction = each.value.autoscale_override != null ? coalesce(each.value.autoscale_override.scaling_rule.inc_scale_direction, var.app_service_plan.autoscale.scaling_rule.inc_scale_direction) : var.app_service_plan.autoscale.scaling_rule.inc_scale_direction
  inc_scale_type      = each.value.autoscale_override != null ? coalesce(each.value.autoscale_override.scaling_rule.inc_scale_type, var.app_service_plan.autoscale.scaling_rule.inc_scale_type) : var.app_service_plan.autoscale.scaling_rule.inc_scale_type
  inc_scale_value     = each.value.autoscale_override != null ? coalesce(each.value.autoscale_override.scaling_rule.inc_scale_value, var.app_service_plan.autoscale.scaling_rule.inc_scale_value) : var.app_service_plan.autoscale.scaling_rule.inc_scale_value
  inc_scale_cooldown  = each.value.autoscale_override != null ? coalesce(each.value.autoscale_override.scaling_rule.inc_scale_cooldown, var.app_service_plan.autoscale.scaling_rule.inc_scale_cooldown) : var.app_service_plan.autoscale.scaling_rule.inc_scale_cooldown

  dec_operator        = each.value.autoscale_override != null ? coalesce(each.value.autoscale_override.scaling_rule.dec_operator, var.app_service_plan.autoscale.scaling_rule.dec_operator) : var.app_service_plan.autoscale.scaling_rule.dec_operator
  dec_threshold       = each.value.autoscale_override != null ? coalesce(each.value.autoscale_override.scaling_rule.dec_threshold, var.app_service_plan.autoscale.scaling_rule.dec_threshold) : var.app_service_plan.autoscale.scaling_rule.dec_threshold
  dec_scale_direction = each.value.autoscale_override != null ? coalesce(each.value.autoscale_override.scaling_rule.dec_scale_direction, var.app_service_plan.autoscale.scaling_rule.dec_scale_direction) : var.app_service_plan.autoscale.scaling_rule.dec_scale_direction
  dec_scale_type      = each.value.autoscale_override != null ? coalesce(each.value.autoscale_override.scaling_rule.dec_scale_type, var.app_service_plan.autoscale.scaling_rule.dec_scale_type) : var.app_service_plan.autoscale.scaling_rule.dec_scale_type
  dec_scale_value     = each.value.autoscale_override != null ? coalesce(each.value.autoscale_override.scaling_rule.dec_scale_value, var.app_service_plan.autoscale.scaling_rule.dec_scale_value) : var.app_service_plan.autoscale.scaling_rule.dec_scale_value
  dec_scale_cooldown  = each.value.autoscale_override != null ? coalesce(each.value.autoscale_override.scaling_rule.dec_scale_cooldown, var.app_service_plan.autoscale.scaling_rule.dec_scale_cooldown) : var.app_service_plan.autoscale.scaling_rule.dec_scale_cooldown
}
