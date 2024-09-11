module "subnet" {
  source = ".//modules/subnet"

  name                                         = module.config.names.subnet
  vnet_name                                    = module.vnet.name
  vnet_address_space                           = var.vnet.vnet_address_space
  resource_group_name                          = module.baseline.resource_group_names[var.subnet.resource_group_key]
  location                                     = module.baseline.resource_group_locations[var.subnet.resource_group_key]
  is_private_endpoint_network_policies_enabled = var.subnet.is_private_endpoint_network_policies_enabled
  network_security_group_id                    = module.nsg.id

  tags = var.tags

}
