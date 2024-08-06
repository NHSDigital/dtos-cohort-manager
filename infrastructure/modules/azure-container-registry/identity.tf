
resource "azurerm_user_assigned_identity" "uai" {
  location            = var.location
  resource_group_name = var.resource_group_name
  name                = var.uai_name
}

# create role assignment for acr
resource "azurerm_role_assignment" "ra" {
  scope                = azurerm_container_registry.acr.id
  role_definition_name = "AcrPush"
  principal_id         = azurerm_user_assigned_identity.uai.principal_id
}
