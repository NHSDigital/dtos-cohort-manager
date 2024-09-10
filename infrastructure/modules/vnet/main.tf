/*--------------------------------------------------------------------------------------------------
  VNets
--------------------------------------------------------------------------------------------------*/

resource "azurerm_virtual_network" "vnet" {
  name                = var.name
  resource_group_name = var.resource_group_name
  address_space       = var.vnet_address_space
  location            = var.location

  tags = var.tags
}


resource "azurerm_network_watcher" "network_watcher" {
  name                = "${var.name}-network-watcher"
  location            = azurerm_virtual_network.vnet.location
  resource_group_name = var.resource_group_name
}
