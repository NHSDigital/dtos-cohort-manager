# Log alert to monitor for CaaS file not being received every 26 hours
resource "azurerm_monitor_scheduled_query_rules_alert" "caas_file_not_received" {
  count = var.features.alerts_enabled ? 1 : 0

  name                = "${module.storage["file_exceptions-${local.primary_region}"].storage_account_name}-caas-file-not-received"
  resource_group_name = azurerm_resource_group.monitoring.name
  location            = azurerm_resource_group.monitoring.location
  description         = "Alert when a CaaS file is not received in the last 26 hours"

  # The resource ID of the Log Analytics Workspace (or other log source)
  data_source_id = data.terraform_remote_state.audit.outputs.log_analytics_workspace_id[local.primary_region]

  # How often the log query is run (in minutes). Must be <= time_window.
  frequency = 60

  # The time range (in minutes) over which the log data is queried (26 hours in minutes = 1560)
  time_window = 1560

  # The Kusto Query Language (KQL) query to run
  query = <<-EOT
    StorageBlobLogs
    | where AccountName == "${module.storage["file_exceptions-${local.primary_region}"].storage_account_name}"
    | where ObjectKey contains("/${module.storage["file_exceptions-${local.primary_region}"].storage_account_name}/inbound/")
    | where OperationName == "PutBlob"
    | summarize Count = count()
    | where Count > 0
  EOT

  # The type of query results. Use 'ResultCount' for a simple count-based alert.
  query_type = "ResultCount"

  # Alert severity (0 to 4, where 0 is the highest severity)
  severity = 2
  enabled  = true

  # The trigger condition for the alert
  trigger {
    operator  = "LessThan"
    threshold = 1
  }

  # Action to take when the alert fires (e.g., email, webhook, runbook)
  action {
    # List of Action Group resource IDs
    action_group  = [module.monitor_action_group_performance[0].monitor_action_group.id]
    email_subject = "Log Alert Fired: Missing CaaS File Detected"
  }
}


# Log alert to monitor for DB Backup not finished in 48 hours
resource "azurerm_monitor_scheduled_query_rules_alert" "db_backup_not_created" {
  count = var.features.alerts_enabled ? 1 : 0

  name                = "${data.terraform_remote_state.audit.outputs.storage_account_audit.sqlbackups.name}-${local.primary_region.name}-db-backup-not-created"
  resource_group_name = azurerm_resource_group.monitoring.name
  location            = azurerm_resource_group.monitoring.location
  description         = "Alert when a DB backup have not been created for 48 hours"

  # The resource ID of the Log Analytics Workspace (or other log source)
  data_source_id = data.terraform_remote_state.audit.outputs.log_analytics_workspace_id[local.primary_region]

  # How often the log query is run (in minutes). Must be <= time_window.
  frequency = 60

  # The time range (in minutes) over which the log data is queried (48 hours in minutes = 2880)
  time_window = 2880

  # The Kusto Query Language (KQL) query to run
  query = <<-EOT
    StorageBlobLogs
    | where AccountName == "${data.terraform_remote_state.audit.outputs.storage_account_audit.sqlbackups.name}-${local.primary_region.name}"
    | where ObjectKey contains("/${data.terraform_remote_state.audit.outputs.storage_account_audit.sqlbackups.name}-${local.primary_region.name}/sql-backups-immutable/")
    | where OperationName == "PutBlockList"
    | summarize Count = count()
    | where Count > 0
  EOT

  # The type of query results. Use 'ResultCount' for a simple count-based alert.
  query_type = "ResultCount"

  # Alert severity (0 to 4, where 0 is the highest severity)
  severity = 1
  enabled  = true

  # The trigger condition for the alert
  trigger {
    operator  = "LessThan"
    threshold = 1
  }

  # Action to take when the alert fires (e.g., email, webhook, runbook)
  action {
    # List of Action Group resource IDs
    action_group  = [module.monitor_action_group_performance[0].monitor_action_group.id]
    email_subject = "Log Alert Fired: No DB backup for the past 48 hours"
  }
}


# Log alert to monitor for DB Backup not finished in 48 hours
resource "azurerm_monitor_scheduled_query_rules_alert" "db_backup_not_created" {
  count = var.features.alerts_enabled ? 1 : 0

  name                = "${data.terraform_remote_state.audit.outputs.storage_account_audit["sqlbackups-${local.primary_region}"].name}-db-backup-not-created"
  resource_group_name = azurerm_resource_group.monitoring.name
  location            = azurerm_resource_group.monitoring.location
  description         = "Alert when a DB backup have not been created for 48 hours"

  # The resource ID of the Log Analytics Workspace (or other log source)
  data_source_id = data.terraform_remote_state.audit.outputs.log_analytics_workspace_id[local.primary_region]

  # How often the log query is run (in minutes). Must be <= time_window.
  frequency = 60

  # The time range (in minutes) over which the log data is queried (48 hours in minutes = 2880)
  time_window = 2880

  # The Kusto Query Language (KQL) query to run
  query = <<-EOT
    StorageBlobLogs
    | where AccountName == "${data.terraform_remote_state.audit.outputs.storage_account_audit["sqlbackups-${local.primary_region}"].name}"
    | where ObjectKey contains("/${data.terraform_remote_state.audit.outputs.storage_account_audit["sqlbackups-${local.primary_region}"].name}/sql-backups-immutable/")
    | where OperationName == "PutBlockList"
    | summarize Count = count()
    | where Count > 0
  EOT

  # The type of query results. Use 'ResultCount' for a simple count-based alert.
  query_type = "ResultCount"

  # Alert severity (0 to 4, where 0 is the highest severity)
  severity = 1
  enabled  = true

  # The trigger condition for the alert
  trigger {
    operator  = "LessThan"
    threshold = 1
  }

  # Action to take when the alert fires (e.g., email, webhook, runbook)
  action {
    # List of Action Group resource IDs
    action_group  = [module.monitor_action_group_performance[0].monitor_action_group.id]
    email_subject = "Log Alert Fired: No DB backup for the past 48 hours"
  }
}
