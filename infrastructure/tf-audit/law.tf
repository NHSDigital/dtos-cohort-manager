module "log_analytics_workspace" {
  source = ".//modules/log-analytics-workspace"

  names = module.config.names

  name_suffix = var.law.name_suffix
  location    = module.baseline.resource_group_locations_audit[var.law.resource_group_key]

  law_sku        = var.law.law_sku
  retention_days = var.law.retention_days

  audit_resource_group_name = module.baseline.resource_group_names_audit[var.law.audit_resource_group_key]

  tags = var.tags

}