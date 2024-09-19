resource "azurerm_subnet" "subnet" {
  name                 = var.name
  resource_group_name  = var.resource_group_name
  address_prefixes     = var.address_prefixes
  virtual_network_name = var.vnet_name

  default_outbound_access_enabled = var.default_outbound_access_enabled

  dynamic "delegation" {
    for_each = var.delegation_name != "" ? [1] : []
    content {
      name = var.delegation_name

      service_delegation {
        name    = var.service_delegation_name
        actions = var.service_delegation_actions
      }
    }
  }

  private_endpoint_network_policies = var.private_endpoint_network_policies
}


module "nsg" {
  source = "../../modules/network-security-group"

  name                = var.network_security_group_name
  resource_group_name = var.resource_group_name
  location            = var.location
  nsg_rules           = var.network_security_group_nsg_rules

  tags = var.tags
}

resource "azurerm_subnet_network_security_group_association" "subnet_nsg_association" {
  subnet_id                 = azurerm_subnet.subnet.id
  network_security_group_id = module.nsg.id
}
