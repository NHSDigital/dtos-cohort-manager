module "app_insights_audit" {

  # Source location updated to use the git:: prefix to avoid URL encoding issues - note // between the URL and the path is required
  source = "git::https://github.com/NHSDigital/dtos-devops-templates.git//infrastructure/modules/app-insights?ref=2c0c8f52681cd1bd2aef4b722cdcb1a8353bca2e"

  name             = module.regions_config[local.primary_region].names.app-insights
  location         = module.baseline.resource_group_locations_audit[var.app_insights.resource_group_key]
  appinsights_type = var.app_insights.appinsights_type

  log_analytics_workspace_id = module.log_analytics_workspace_audit.id

  resource_group_name = module.baseline.resource_group_names_audit[var.app_insights.audit_resource_group_key]
  tags                = var.tags

}
