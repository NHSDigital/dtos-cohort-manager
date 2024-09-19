resource "azurerm_virtual_network_peering" "peering" {
  name                         = var.name
  resource_group_name          = var.resource_group_name
  virtual_network_name         = var.vnet_name
  remote_virtual_network_id    = var.remote_vnet_id
  allow_virtual_network_access = var.allow_virtual_network_access
  allow_forwarded_traffic      = var.allow_forwarded_traffic

  # `allow_gateway_transit` must be set to false for vnet Global Peering
  allow_gateway_transit = var.allow_gateway_transit

}
