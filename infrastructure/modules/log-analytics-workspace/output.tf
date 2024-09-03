output "id" {
  value = azurerm_log_analytics_workspace.law.id
}

output "name" {
  value = azurerm_log_analytics_workspace.law.name
}

### audit sub
output "audit_id" {
  value = azurerm_log_analytics_workspace.law_audit.id
}

output "audit_name" {
  value = azurerm_log_analytics_workspace.law_audit.name
}
