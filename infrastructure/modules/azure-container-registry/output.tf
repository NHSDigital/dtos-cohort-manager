
output "login_server" {
  value = azurerm_container_registry.acr[count.index].login_server
}

output "mi_id" {
  value = azurerm_user_assigned_identity.uai[count.index].id
}

output "mi_client_id" {
  value = azurerm_user_assigned_identity.uai[count.index].client_id
}
