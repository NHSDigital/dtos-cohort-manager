output "function_app_sami_id" {
  description = "The Principal ID of the System Assigned Managed Service Identity that is configured on this Linux Function App."
  value       = { for k, function in azurerm_linux_function_app.function : k => function.identity.0.principal_id }
}
