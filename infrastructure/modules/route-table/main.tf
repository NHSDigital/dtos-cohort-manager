resource "azurerm_route_table" "route_table" {
  name                = var.name
  location            = var.location
  resource_group_name = var.resource_group_name

  tags = var.tags

  bgp_route_propagation_enabled = var.bgp_route_propagation_enabled

  dynamic "route" {
    for_each = var.routes
    content {
      name                   = route.value.name
      address_prefix         = route.value.address_prefix
      next_hop_type          = route.value.next_hop_type
      next_hop_in_ip_address = route.value.next_hop_in_ip_address
    }
  }
}

resource "azurerm_subnet_route_table_association" "route_table_association" {
  count = length(var.subnet_ids)

  subnet_id      = var.subnet_ids[count.index]
  route_table_id = azurerm_route_table.route_table.id
}
