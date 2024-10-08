### audit sub

output "ai_connection_string_audit" {
  value = var.features["app_insighte_enabled"] ? element(azurerm_application_insights.appins_audit[*].connection_string, 0) : null
  #value = azurerm_application_insights.appins_audit.connection_string
}
