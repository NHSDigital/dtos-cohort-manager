module "app_insights_audit" {
  for_each = { for key, val in var.regions : key => val if val.is_primary_region }

  source = "../../../dtos-devops-templates/infrastructure/modules/app-insights"

  name             = module.regions_config[each.key].names.app-insights
  location         = each.key
  appinsights_type = var.app_insights.appinsights_type

  log_analytics_workspace_id = module.log_analytics_workspace_audit[each.key].id

  resource_group_name = azurerm_resource_group.audit[each.key].name
  tags                = var.tags

}
