module "log_analytics_workspace_audit" {
  for_each = { for key, val in var.regions : key => val if val.is_primary_region }

  source = "../../../dtos-devops-templates/infrastructure/modules/log-analytics-workspace"

  name     = module.regions_config[each.key].names.log-analytics-workspace
  location = each.key

  law_sku        = var.law.law_sku
  retention_days = var.law.retention_days

  resource_group_name = azurerm_resource_group.audit[each.key].name

  tags = var.tags
}
