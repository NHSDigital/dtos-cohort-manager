module "app_insights" {
  count = var.features["app_insighte_enabled"] ? 1 : 0

  source = ".//modules/app-insights"

  providers = {
    azurerm.audit = azurerm.audit
  }

  app_insights_enabled = var.features.app_insights_enabled

  names = module.config.names

  name_suffix         = var.app_insights.name_suffix
  resource_group_name = module.baseline.resource_group_names[var.app_insights.resource_group_key]
  location            = module.baseline.resource_group_locations[var.app_insights.resource_group_key]
  appinsights_type    = var.app_insights.appinsights_type

  #law_id       = module.log_analytics_workspace.id[0]
  audit_law_id = module.log_analytics_workspace.audit_id[0]

  audit_resource_group_name = module.baseline.resource_group_names_audit[var.app_insights.audit_resource_group_key]
  tags                      = var.tags

}
