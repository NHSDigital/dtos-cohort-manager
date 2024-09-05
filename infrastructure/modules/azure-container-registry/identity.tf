
resource "azurerm_user_assigned_identity" "uai" {

  count = var.deployacr ? 1 : 0

  location            = var.location
  resource_group_name = var.resource_group_name
  name                = var.uai_name
}

# create role assignment for acr
resource "azurerm_role_assignment" "ra" {

  count = var.deployacr ? 1 : 0

  scope                = azurerm_container_registry.acr[count.index].id
  role_definition_name = "AcrPush"
  principal_id         = azurerm_user_assigned_identity.uai.principal_id
}
