/*--------------------------------------------------------------------------------------------------
  Private Endpoint Subnets
--------------------------------------------------------------------------------------------------*/

# Define the subnet
resource "azurerm_subnet" "subnet" {
  name                 = var.name
  resource_group_name  = var.resource_group_name
  virtual_network_name = var.vnet_name
  address_prefixes     = var.vnet_address_space

  # Comment out or remove the unsupported arguments
  # network_security_group_id = azurerm_network_security_group.this.id
  # tags = var.tags
}

resource "azurerm_subnet_network_security_group_association" "subnet_nsg_association" {
  subnet_id                 = azurerm_subnet.subnet.id
  network_security_group_id = var.network_security_group_id
}
