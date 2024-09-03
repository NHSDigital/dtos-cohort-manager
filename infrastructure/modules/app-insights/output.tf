
output "ai_connection_string" {
  value = azurerm_application_insights.appins.connection_string
}

### audit sub

output "ai_connection_string_audit" {
  value = azurerm_application_insights.appins_audit.connection_string
}
