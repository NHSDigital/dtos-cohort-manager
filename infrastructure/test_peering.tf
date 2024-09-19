module "vnet_demo_remote" {
  source = "./modules/vnet"

  name                = "${module.config.names.virtual-network}-remote"
  vnet_address_space  = ["10.2.0.0/16"]
  resource_group_name = module.baseline.resource_group_names[var.vnet.resource_group_key]
  location            = module.baseline.resource_group_locations[var.vnet.resource_group_key]

  tags = var.tags
}

# Peering spoke to hub
module "peering" {
  source = "./modules/vnet-peering"

  name                = "vnet-peering-demo"
  resource_group_name = module.baseline.resource_group_names[var.vnet.resource_group_key]
  vnet_name           = module.vnet_demo.name
  remote_vnet_id      = module.vnet_demo_remote.vnet.id

  allow_virtual_network_access           = true
  allow_forwarded_traffic                = true
  allow_gateway_transit                  = false
  peer_complete_virtual_networks_enabled = true
  use_remote_gateways                    = false

}

# Peering hub to spoke
module "peering_remote" {
  source = "./modules/vnet-peering"

  name                = "vnet-peering-demo-remote"
  resource_group_name = module.baseline.resource_group_names[var.vnet.resource_group_key]
  vnet_name           = module.vnet_demo_remote.name
  remote_vnet_id      = module.vnet_demo.vnet.id

  allow_virtual_network_access           = true
  allow_forwarded_traffic                = false
  allow_gateway_transit                  = false
  peer_complete_virtual_networks_enabled = true
  use_remote_gateways                    = false

}
