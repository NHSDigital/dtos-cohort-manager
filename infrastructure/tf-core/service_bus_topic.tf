module "azure_service_bus" {
  for_each = local.azure_service_bus_map

  source = "../../../dtos-devops-templates/infrastructure/modules/service-bus"

  servicebus_namespace_name = "${module.regions_config[each.value.region].names.service-bus-namespace}-${each.value.service_bus_key}"
  resource_group_name       = azurerm_resource_group.core[each.value.region].name
  location                  = each.value.region
  capacity                  = each.value.capacity
  sku_tier                  = each.value.sku_tier

  servicebus_topic_map = each.value.topics

  # Private Endpoint Configuration if enabled
  private_endpoint_properties = var.features.private_endpoints_enabled ? {
    # NOTE: Event Grids also use the Service Bus private DNS zone, and as the first Event Grid was created prior to the first Service Bus,
    # the private DNS zone is named after the former resource type rather than the latter (which would be more correct).
    private_dns_zone_ids                 = [data.terraform_remote_state.hub.outputs.private_dns_zones["${each.value.region}-event_hub"].id]
    private_endpoint_enabled             = var.features.private_endpoints_enabled
    private_endpoint_subnet_id           = module.subnets["${module.regions_config[each.value.region].names.subnet}-pep"].id
    private_endpoint_resource_group_name = azurerm_resource_group.rg_private_endpoints[each.value.region].name
    private_service_connection_is_manual = var.features.private_service_connection_is_manual
  } : null

  tags = var.tags
}

locals {

  azure_service_bus_object_list = flatten([
    for region in keys(var.regions) : [
      for service_bus_key, service_bus_details in var.service_bus : merge(
        {
          region          = region
          service_bus_key = service_bus_key
        },
        service_bus_details
      )
    ]
  ])

  # ...then project the list of objects into a map with unique keys (combining the iterators), for consumption by a for_each meta argument
  azure_service_bus_map = {
    for object in local.azure_service_bus_object_list : "${object.service_bus_key}-${object.region}" => object
  }
}

