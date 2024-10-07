module "app_insights_audit" {
  source = ".//modules/app-insights"

  name             = module.config.names.app-insights-web
  location         = module.baseline.resource_group_locations_audit[var.app_insights.resource_group_key]
  appinsights_type = var.app_insights.appinsights_type

  log_analytics_workspace_id = module.log_analytics_workspace_audit.id

  resource_group_name = module.baseline.resource_group_names_audit[var.app_insights.audit_resource_group_key]
  tags                = var.tags

}
