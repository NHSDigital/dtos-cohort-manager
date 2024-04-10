output "ai_instrumentation_key" {
  value = azurerm_application_insights.appins.instrumentation_key
}

output "ai_connection_string" {
  value = azurerm_application_insights.appins.connection_string
}
