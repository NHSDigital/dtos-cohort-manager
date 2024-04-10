module "app_insights" {
  source = ".//modules/app-insights"

  names = module.config.names

  name_suffix         = var.app_insights.name_suffix
  resource_group_name = module.baseline.resource_group_names[var.app_insights.resource_group_key]
  location            = module.baseline.resource_group_locations[var.app_insights.resource_group_key]
  appinsights_type    = var.app_insights.appinsights_type

  law_id = module.log_analytics_workspace.id

  tags = var.tags

}
