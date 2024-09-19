module "vnet_demo" {
  source = "./modules/vnet"

  name                = module.config.names.virtual-network
  vnet_address_space  = var.vnet.vnet_address_space
  resource_group_name = module.baseline.resource_group_names[var.vnet.resource_group_key]
  location            = module.baseline.resource_group_locations[var.vnet.resource_group_key]

  tags = var.tags

}

module "nsg_demo" {
  source = "./modules/network-security-group"

  name                = module.config.names.network-security-group
  resource_group_name = module.baseline.resource_group_names[var.vnet.resource_group_key]
  location            = module.baseline.resource_group_locations[var.network_security_group.resource_group_key]
  nsg_rules           = var.network_security_group.nsg_rules

  tags = var.tags
}

module "subnet_without_delegation" {
  source = "./modules/subnet"

  name                              = "subnet-without-delegation"
  location                          = module.baseline.resource_group_locations[var.network_security_group.resource_group_key]
  network_security_group_name       = "nsg-subnet-without-delegation"
  resource_group_name               = module.baseline.resource_group_names[var.vnet.resource_group_key]
  vnet_name                         = module.vnet_demo.name
  address_prefixes                  = ["10.1.1.0/24"]
  default_outbound_access_enabled   = true
  private_endpoint_network_policies = "Disabled" # Default as per compliance requirements

  tags = var.tags
}

module "subnet_with_delegation" {
  source = "./modules/subnet"

  name                              = "subnet-with-delegation"
  location                          = module.baseline.resource_group_locations[var.network_security_group.resource_group_key]
  network_security_group_name       = "nsg-subnet-with-delegation"
  resource_group_name               = module.baseline.resource_group_names[var.vnet.resource_group_key]
  vnet_name                         = module.vnet_demo.name
  address_prefixes                  = ["10.1.2.0/24"]
  default_outbound_access_enabled   = true
  private_endpoint_network_policies = "Disabled" # Default as per compliance requirements

  delegation_name            = "my-delegation"
  service_delegation_name    = "Microsoft.App/environments"
  service_delegation_actions = ["Microsoft.Network/virtualNetworks/subnets/action"]

  tags = var.tags
}
