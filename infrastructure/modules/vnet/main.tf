resource "azurerm_virtual_network" "vnet" {
  name                = var.name
  resource_group_name = var.resource_group_name
  address_space       = var.vnet_address_space
  location            = var.location
}
