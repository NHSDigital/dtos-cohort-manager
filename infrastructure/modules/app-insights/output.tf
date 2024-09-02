output "ai_instrumentation_key" {
  value = azurerm_application_insights.appins.instrumentation_key
}

output "ai_connection_string" {
  value = azurerm_application_insights.appins.connection_string
}


### audit sub

output "ai_instrumentation_key_audit" {
  value = azurerm_application_insights.appins_audit.instrumentation_key
}

output "ai_connection_string_audit" {
  value = azurerm_application_insights.appins_audit.connection_string
}
