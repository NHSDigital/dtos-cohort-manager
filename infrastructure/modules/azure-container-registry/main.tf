
resource "azurerm_container_registry" "acr" {

  count = var.deployacr ? 1 : 0

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
