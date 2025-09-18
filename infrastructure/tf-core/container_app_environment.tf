module "container-app-environment" {
  for_each = local.container_app_environments_map

  source = "../../../dtos-devops-templates/infrastructure/modules/container-app-environment"

  # Even though we are not enabling public ingress, the structure of the template module requires the provider for the private DNS zone subscription to be supplied because we need to create the Private DNS zone entry.
  providers = {
    azurerm     = azurerm
    azurerm.dns = azurerm.hub
  }

  name                 = "${module.regions_config[each.value.region].names.container-app-environment}-${lower(each.value.container_app_environment)}"
  resource_group_name  = azurerm_resource_group.core[each.value.region].name
  location             = each.value.region
  custom_infra_rg_name = each.value.use_custom_infra_rg_name == true ? "${azurerm_resource_group.core[each.value.region].name}-cae-${each.value.container_app_environment}" : null

  log_analytics_workspace_id = data.terraform_remote_state.audit.outputs.log_analytics_workspace_id[local.primary_region]
  vnet_integration_subnet_id = module.subnets["${module.regions_config[each.value.region].names.subnet}-container-app-${lower(each.value.container_app_environment)}"].id
  workload_profile           = each.value.workload_profile
  zone_redundancy_enabled    = each.value.zone_redundancy_enabled
  private_dns_zone_rg_name   = "rg-hub-${var.environment_hub}-uks-private-dns-zones"
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
