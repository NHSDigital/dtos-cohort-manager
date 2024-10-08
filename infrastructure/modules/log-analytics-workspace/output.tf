### audit sub
output "audit_id" {
  value = var.law_enabled ? element(azurerm_log_analytics_workspace.law_audit[*].id, 0) : null
}

output "audit_name" {
  value = var.law_enabled ? element(azurerm_log_analytics_workspace.law_audit[*].name, 0) : null
}
