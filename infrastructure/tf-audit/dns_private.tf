/*--------------------------------------------------------------------------------------------------
  Private DNS zones
--------------------------------------------------------------------------------------------------*/

locals {
  private_dns_zones = {
    app_insights                = var.private_dns_zones.is_app_insights_private_dns_zone_enabled ? "privatelink.monitor.azure.com" : null
    automation                  = var.private_dns_zones.is_app_insights_private_dns_zone_enabled ? "privatelink.agentsvc.azure-automation.net" : null
    operations_data_store       = var.private_dns_zones.is_app_insights_private_dns_zone_enabled ? "privatelink.ods.opinsights.azure.com" : null
    operations_management_suite = var.private_dns_zones.is_app_insights_private_dns_zone_enabled ? "privatelink.oms.opinsights.azure.com" : null
    storage_blob                = var.private_dns_zones.is_app_insights_private_dns_zone_enabled ? "privatelink.blob.core.windows.net" : null
  }

  private_dns_zones_obj_list = flatten([
    for region in keys(var.regions) : [
      for description, zone in local.private_dns_zones : {
        region      = region
        description = description
        name        = zone
      } if zone != null
    ]
  ])
  private_dns_zones_map = { for obj in local.private_dns_zones_obj_list : "${obj.region}-${obj.description}" => obj }
}

module "private_dns_zones" {
  for_each = local.private_dns_zones_map

  source = "../../../dtos-devops-templates/infrastructure/modules/private-dns-zone"

  name                = each.value.name
  resource_group_name = azurerm_resource_group.rg_private_endpoints[each.value.region].name
  vnet_id             = module.vnet[each.value.region].vnet.id

  tags = var.tags
}
