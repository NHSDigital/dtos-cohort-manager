
output "storage_account_name" {
  value = azurerm_storage_account.sa.name
}

output "storage_account_primary_access_key" {
  sensitive = true
  value     = azurerm_storage_account.sa.primary_access_key
}

output "sa_fe_primary_connection_string" {
  sensitive = true
  value     = azurerm_storage_account.fileexception.primary_connection_string
}
