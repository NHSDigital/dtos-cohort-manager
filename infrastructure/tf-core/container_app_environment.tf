module "container-app-environment" {
  for_each = local.container_app_environments_map

  source = "../../../dtos-devops-templates/infrastructure/modules/container-app-environment"

  name                = "${module.regions_config[each.value.region].names.container-app-environment}-${lower(each.value.container_app_environment)}"
  resource_group_name = azurerm_resource_group.core[each.value.region].name
  location            = each.value.region

  log_analytics_workspace_id = data.terraform_remote_state.audit.outputs.log_analytics_workspace_id[local.primary_region]
  vnet_integration_subnet_id = module.subnets["${module.regions_config[each.value.region].names.subnet}-container-app-${lower(each.value.container_app_environment)}"].id
  zone_redundancy_enabled    = each.value.zone_redundancy_enabled
}

locals {
  # There are multiple App Service Plans and possibly multiple regions.
  # We cannot nest for loops inside a map, so first iterate all permutations of both as a list of objects...
  container_app_environments_object_list = flatten([
    for region in keys(var.regions) : [
      for container_app_environment, config in var.container_app_environments.instances : merge(
        {
          region                    = region                    # 1st iterator
          container_app_environment = container_app_environment # 2nd iterator
        },
        config # the rest of the key/value pairs for a specific container_app_environment
      )
    ]
  ])

  # ...then project the list of objects into a map with unique keys (combining the iterators), for consumption by a for_each meta argument
  container_app_environments_map = {
    for object in local.container_app_environments_object_list : "${object.container_app_environment}-${object.region}" => object
  }
}
