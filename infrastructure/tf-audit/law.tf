module "log_analytics_workspace_audit" {

  # Source location updated to use the git:: prefix to avoid URL encoding issues - note // between the URL and the path is required
  source = "git::https://github.com/NHSDigital/dtos-devops-templates.git//infrastructure/modules/log-analytics-workspace?ref=2c0c8f52681cd1bd2aef4b722cdcb1a8353bca2e"

  name     = module.regions_config[local.primary_region].names.log-analytics-workspace
  location = module.baseline.resource_group_locations_audit[var.law.resource_group_key]

  law_sku        = var.law.law_sku
  retention_days = var.law.retention_days

  resource_group_name = module.baseline.resource_group_names_audit[var.law.audit_resource_group_key]

  tags = var.tags

}
