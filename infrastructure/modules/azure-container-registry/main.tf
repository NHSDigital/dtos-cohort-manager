

resource "azurerm_user_assigned_identity" "uai" {
  location            = var.location
  resource_group_name = var.resource_group_name
  name                = var.uai_name
}

resource "azurerm_container_registry" "acr" {
  name                = var.names.azure-container-registry
  location            = var.location
  resource_group_name = var.resource_group_name
  sku                 = var.sku
  admin_enabled       = var.admin_enabled

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.uai.id]
  }

  # georeplications {
  #   location                = "East US"
  #   zone_redundancy_enabled = true
  #   tags                    = {}
  # }
}

# create role assignment for acr
resource "azurerm_role_assignment" "ra" {
  scope                = azurerm_container_registry.acr.id
  role_definition_name = "AcrPush"
  principal_id         = azurerm_user_assigned_identity.uai.principal_id
}
