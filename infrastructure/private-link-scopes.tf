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

  name                = "${module.regions_config[each.key].names.log-analytics-workspace}-${var.law.name_suffix}-ampls-service"
  resource_group_name = data.terraform_remote_state.hub.outputs.vnets_hub[each.key].vnet.resource_group_name

  linked_resource_id = module.log_analytics_workspace.id
  scope_name         = module.private_link_scope[each.key].scope_name
  #scope_name         = "${module.regions_config[each.key].names.log-analytics-workspace}-${var.law.name_suffix}-ampls"
}

# Create the private link scope in the spoke subscription
module "private_link_scope" {
  for_each = {
    for key, region in var.regions :
    key => region if var.features.private_endpoints_enabled
  }

  source = ".//modules/private-link-scope"

  name                = "${module.regions_config[each.key].names.log-analytics-workspace}-${var.law.name_suffix}-ampls"
  resource_group_name = module.baseline.resource_group_names[var.law.resource_group_key]

  ingestion_access_mode = "PrivateOnly"
  query_access_mode     = "Open"

  tags = var.tags

}
