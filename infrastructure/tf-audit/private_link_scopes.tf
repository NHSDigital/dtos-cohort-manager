# Create the private link service for Application Insights and Log Analytics
module "private_link_scoped_service_app_insights" {
  for_each = var.features.private_endpoints_enabled ? var.regions : {}

  source = "../../../dtos-devops-templates/infrastructure/modules/private-link-scoped-service"

  name                = "${module.regions_config[each.key].names.log-analytics-workspace}-ampls-service-app-insights"
  resource_group_name = azurerm_resource_group.rg_vnet[each.key].name

  linked_resource_id = module.app_insights_audit[each.key].id
  scope_name         = module.private_link_scope[each.key].scope_name
}

module "private_link_scoped_service_law" {
  for_each = var.features.private_endpoints_enabled ? var.regions : {}

  source = "../../../dtos-devops-templates/infrastructure/modules/private-link-scoped-service"

  name                = "${module.regions_config[each.key].names.log-analytics-workspace}-ampls-service-law"
  resource_group_name = azurerm_resource_group.rg_vnet[each.key].name

  linked_resource_id = module.log_analytics_workspace_audit[each.key].id
  scope_name         = module.private_link_scope[each.key].scope_name
}

# Create the private link scope in the spoke subscription
module "private_link_scope" {
  for_each = var.features.private_endpoints_enabled ? var.regions : {}

  source = "../../../dtos-devops-templates/infrastructure/modules/private-link-scope"

  name                = module.regions_config[each.key].names.log-analytics-workspace
  resource_group_name = azurerm_resource_group.rg_vnet[each.key].name
  location            = each.key

  ingestion_access_mode = "PrivateOnly"
  query_access_mode     = "Open"

  # Private Endpoint Configuration if enabled
  private_endpoint_properties = var.features.private_endpoints_enabled ? {
    private_dns_zone_ids = [
      data.terraform_remote_state.hub.outputs.private_dns_zone_app_insight[each.key].private_dns_zone.id,
      data.terraform_remote_state.hub.outputs.private_dns_zone_azure_automation[each.key].private_dns_zone.id,
      data.terraform_remote_state.hub.outputs.private_dns_zone_od_insights[each.key].private_dns_zone.id,
      data.terraform_remote_state.hub.outputs.private_dns_zone_op_insights[each.key].private_dns_zone.id,
      data.terraform_remote_state.hub.outputs.private_dns_zone_storage_blob[each.key].private_dns_zone.id
    ]
    private_endpoint_enabled             = var.features.private_endpoints_enabled
    private_endpoint_subnet_id           = module.subnets["${module.regions_config[each.key].names.subnet}-pep"].id
    private_endpoint_resource_group_name = azurerm_resource_group.rg_private_endpoints[each.key].name
    private_service_connection_is_manual = var.features.private_service_connection_is_manual
  } : null

  tags = var.tags

}
