module "subnet" {
  for_each = var.regions

  source = ".//modules/subnet"

  name                                         = "${var.environment}-subnet-${each.value.caf_short_name}"
  vnet_name                                    = module.vnet[each.key].name
  vnet_address_space                           = each.value.vnet_address_space
  resource_group_name                          = module.baseline.resource_group_names[var.subnet.resource_group_key]
  location                                     = each.key
  is_private_endpoint_network_policies_enabled = var.subnet.is_private_endpoint_network_policies_enabled
  network_security_group_id                    = module.nsg[each.key].id

  tags = var.tags

}
