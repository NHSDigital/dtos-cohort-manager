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
  network_security_group_id = azurerm_network_security_group.this.id
}

resource "azurerm_network_security_group" "this" {
  name                = "nsg"
  location            = var.location
  resource_group_name = var.resource_group_name

  # Dynamically create security rules from the variable
  dynamic "security_rule" {
    for_each = var.default_nsg_rules
    content {
      name                       = security_rule.value.name
      priority                   = security_rule.value.priority
      direction                  = security_rule.value.direction
      access                     = security_rule.value.access
      protocol                   = security_rule.value.protocol
      source_port_range          = security_rule.value.source_port_range
      destination_port_range     = security_rule.value.destination_port_range
      source_address_prefix      = security_rule.value.source_address_prefix
      destination_address_prefix = security_rule.value.destination_address_prefix
    }
  }
}
