# Create the private link service for Application Insights and Log Analytics
module "private_link_scoped_service_app_insights" {
  for_each = var.features.private_endpoints_enabled ? var.regions : {}

  source = "../../../dtos-devops-templates/infrastructure/modules/private-link-scoped-service"

  name                = "${module.regions_config[each.key].names.log-analytics-workspace}-ampls-service-app-insights"
  resource_group_name = azurerm_resource_group.rg_vnet[each.key].name

  linked_resource_id = data.terraform_remote_state.audit.outputs.application_insights_id[local.primary_region]
  scope_name         = module.private_link_scope[each.key].scope_name
}

module "private_link_scoped_service_law" {
  for_each = var.features.private_endpoints_enabled ? var.regions : {}

  source = "../../../dtos-devops-templates/infrastructure/modules/private-link-scoped-service"

  name                = "${module.regions_config[each.key].names.log-analytics-workspace}-ampls-service-law"
  resource_group_name = azurerm_resource_group.rg_vnet[each.key].name

  linked_resource_id = module.log_analytics_workspace_audit[local.primary_region].id
  scope_name         = module.private_link_scope[each.key].scope_name
}

# Create the private link scope in the spoke subscription
module "private_link_scope" {
  for_each = var.features.private_endpoints_enabled ? var.regions : {}

  providers = {
    azurerm = azurerm.audit
  }

  source = "../../../dtos-devops-templates/infrastructure/modules/private-link-scope"

  name                = module.regions_config[each.key].names.log-analytics-workspace
  resource_group_name = azurerm_resource_group.rg_vnet[each.key].name
  location            = each.key

  ingestion_access_mode = "PrivateOnly"
  query_access_mode     = "Open"

  # Private Endpoint Configuration if enabled
  private_endpoint_properties = var.features.private_endpoints_enabled ? {
    private_dns_zone_ids = [
      for zone in module.private_dns_zones : zone.id
    ]
    private_endpoint_enabled             = var.features.private_endpoints_enabled
    private_endpoint_subnet_id           = data.terraform_remote_state.audit.outputs.subnet_pep_id[each.key]
    private_endpoint_resource_group_name = data.terraform_remote_state.audit.outputs.private_endpoint_rg_name[each.key]
    private_service_connection_is_manual = var.features.private_service_connection_is_manual
  } : null

  tags = var.tags

}
