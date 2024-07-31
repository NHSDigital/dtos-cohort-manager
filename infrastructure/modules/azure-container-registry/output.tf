
output "login_server" {
  value = azurerm_container_registry.acr.login_server
}

output "mi_id" {
  value = azurerm_user_assigned_identity.uai.id
}

output "mi_client_id" {
  value = azurerm_user_assigned_identity.uai.client_id
}
