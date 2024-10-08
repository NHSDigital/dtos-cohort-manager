### audit sub
output "audit_id" {
  #value = azurerm_log_analytics_workspace.law_audit[0].id
  value = var.features["law_enabled"] ? element(azurerm_log_analytics_workspace.law_audit[*].id, 0) : null
}

output "audit_name" {
  #value = azurerm_log_analytics_workspace.law_audit[0].name
  value = var.features["law_enabled"] ? element(azurerm_log_analytics_workspace.law_audit[*].name, 0) : null
}
