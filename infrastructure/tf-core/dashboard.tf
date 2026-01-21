locals {
  # There are multiple Dashboards and possibly multiple regions.
  # We cannot nest for loops inside a map, so first iterate all permutations of both as a list of objects...
  dashboard_object_list = flatten([
    for region in keys(var.regions) : [
      for dashboard in var.dashboards : merge(
        {
          region    = region    # 1st iterator
          dashboard = dashboard # 2nd iterator
        }
      )
    ]
  ])

  # ...then project the list of objects into a map with unique keys (combining the iterators), for consumption by a for_each meta argument
  dashboard_maps = {
    for object in local.dashboard_object_list : "${object.dashboard.name}-${object.region}" => object
  }
}

module "dashboard" {
  for_each = local.dashboard_maps

  source = "../../../dtos-devops-templates/infrastructure/modules/dashboard"

  name                = "${module.regions_config[each.value.region].names.dashboard}-${lower(each.value.dashboard.name)}"
  resource_group_name = azurerm_resource_group.core[each.value.region].name
  location            = each.value.region

  dashboard_properties = templatefile(each.value.dashboard.path,
    {
      audit_sub_id                     = var.AUDIT_SUBSCRIPTION_ID
      audit_resource_group             = "${module.regions_config[each.value.region].names.resource-group}-audit"
      audit_resource_name_app_insights = module.regions_config[each.value.region].names.app-insights
      core_sub_id                      = var.TARGET_SUBSCRIPTION_ID
      core_resource_group              = module.regions_config[each.value.region].names.resource-group
      core_app_service_plan            = module.regions_config[each.value.region].names.app-service-plan
      linux_web_app                    = module.regions_config[each.value.region].names.linux-web-app
      linux_web_app_name_suffix        = var.linux_web_app.linux_web_app_config.FrontEndUi.name_suffix
    }
  )
}
