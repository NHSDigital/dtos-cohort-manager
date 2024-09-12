module "vnet" {
  source = ".//modules/vnet"

  name                = module.config.names.virtual-network
  vnet_address_space  = var.vnet.vnet_address_space
  resource_group_name = module.baseline.resource_group_names[var.vnet.resource_group_key]
  location            = module.baseline.resource_group_locations[var.vnet.resource_group_key]

  tags = var.tags

}
