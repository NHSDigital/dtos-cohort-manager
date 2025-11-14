
# Azure Monitor alert for DToSDB CPU Percentage higher than 90% for the past 5 minutes
resource "azurerm_monitor_metric_alert" "db_cpu_usage" {
  count = var.features.alerts_enabled ? 1 : 0

  name                = "${var.environment}-${var.sqlserver.dbs.cohman.db_name_suffix}-db_cpu_usage"
  resource_group_name = azurerm_resource_group.monitoring.name
  scopes              = ["${module.azure_sql_server.sql_server_id}/databases/${var.sqlserver.dbs.cohman.db_name_suffix}"]
  description         = "${var.environment} DToSDB - Average CPU Percentage for the past 5 minutes above 90%."

  frequency           = "PT1M"
  window_size         = "PT5M"
  severity            = 2

  criteria {
    metric_namespace = "Microsoft.Sql/servers/databases"
    metric_name      = "cpu_percent"
    aggregation      = "Average"
    operator         = "GreaterThan"
    threshold        = 90

  }

  action {
    action_group_id = [module.monitor_action_group_performance[0].monitor_action_group.id]
  }
}
