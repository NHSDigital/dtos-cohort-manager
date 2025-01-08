# Create the private link service for Application Insights
module "private_link_scoped_service_app_insights" {
  for_each = var.features.private_endpoints_enabled ? var.regions : {}

  source = "../../../dtos-devops-templates/infrastructure/modules/private-link-scoped-service"

  providers = {
    azurerm = azurerm.hub
  }

  name                = "${module.regions_config[each.key].names.log-analytics-workspace}-ampls-service-app-insights"
  resource_group_name = data.terraform_remote_state.hub.outputs.private_endpoint_rg_name[each.key]

  linked_resource_id = module.app_insights_audit[each.key].id
  scope_name         = data.terraform_remote_state.hub.outputs.azure_monitor_private_link_scope_name
}

# Create the private link service for Log Analytics
module "private_link_scoped_service_law" {
  for_each = var.features.private_endpoints_enabled ? var.regions : {}

  source = "../../../dtos-devops-templates/infrastructure/modules/private-link-scoped-service"

  providers = {
    azurerm = azurerm.hub
  }

  name                = "${module.regions_config[each.key].names.log-analytics-workspace}-ampls-service-law"
  resource_group_name = data.terraform_remote_state.hub.outputs.private_endpoint_rg_name[each.key]

  linked_resource_id = module.log_analytics_workspace_audit[each.key].id
  scope_name         = data.terraform_remote_state.hub.outputs.azure_monitor_private_link_scope_name
}
