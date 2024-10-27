# Create the private link service in the hub subscription
module "private_link_scoped_service" {
  for_each = {
    for key, region in var.regions :
    key => region if var.features.private_endpoints_enabled
  }

  source = ".//modules/private-link-scoped-service"

  providers = {
    azurerm = azurerm.hub
  }

  name                = "${module.regions_config[each.key].names.log-analytics-workspace}-ampls-service"
  resource_group_name = azurerm_resource_group.rg_vnet[each.key].name

  linked_resource_id = module.log_analytics_workspace_audit.id
  scope_name         = module.private_link_scope[each.key].scope_name
}

# Create the private link scope in the spoke subscription
module "private_link_scope" {
  for_each = {
    for key, region in var.regions :
    key => region if var.features.private_endpoints_enabled
  }

  source = ".//modules/private-link-scope"

  name                = "${module.regions_config[each.key].names.log-analytics-workspace}-ampls"
  resource_group_name = azurerm_resource_group.rg_vnet[each.key].name

  ingestion_access_mode = "PrivateOnly"
  query_access_mode     = "Open"

  tags = var.tags

}
