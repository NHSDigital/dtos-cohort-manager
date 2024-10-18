resource "azurerm_monitor_diagnostic_setting" "diagnostic_setting" {
  name                       = var.name
  target_resource_id         = var.target_resource_id
  log_analytics_workspace_id = var.log_analytics_workspace_id
  log {
    category = var.log_categories
    enabled  = var.enable_security_audit_logs

    retention_policy {
      enabled = var.logs_retention_policy
      days    = var.logs_retention_days
    }
  }

  metric {
    category = var.metric_categories

    retention_policy {
      enabled = var.metrics_retention_policy
      days    = var.metrics_retention_days
    }
  }

  lifecycle {
    ignore_changes = [log, metric]
  }
}


