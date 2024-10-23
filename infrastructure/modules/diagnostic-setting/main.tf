resource "azurerm_monitor_diagnostic_setting" "diagnostic_setting" {
  name                       = var.name
  target_resource_id         = var.target_resource_id
  log_analytics_workspace_id = var.diagnostic_setting_properties.log_analytics_workspace_id

  log {
    category = var.diagnostic_setting_properties.log_categories
    enabled  = var.diagnostic_setting_properties.diagnostic_setting_audit_logs_enabled
  }

  #   retention_policy {
  #     enabled = var.logs_retention_policy
  #     days    = var.logs_retention_days
  #   }
  # } May have to configure at resource level

  metric {
    category = var.diagnostic_setting_properties.metrics_categories

    # retention_policy {
    #   enabled = var.metrics_retention_policy
    #   days    = var.metrics_retention_days
    # } May have to configure at a resource level
  }

  lifecycle {
    ignore_changes = [log, metric]
  }
}


