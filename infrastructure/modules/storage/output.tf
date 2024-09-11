
output "storage_account_names" {
  description = "The names of the created Storage Accounts"
  value       = { for k, sa in azurerm_storage_account.sa : k => sa.name }
}

output "sa_primary_connection_strings" {
  sensitive = true
  value     = { for k, sa in azurerm_storage_account.sa : k => sa.primary_connection_string }
}
