resource "azurerm_resource_group" "rg_vnet" {
  for_each = var.regions

  name     = "${module.regions_config[each.key].names.resource-group}-networking"
  location = each.key
}

resource "azurerm_resource_group" "rg_private_endpoints" {
  for_each = var.features.private_endpoints_enabled ? var.regions : {}

  name     = "${module.regions_config[each.key].names.resource-group}-private-endpoints"
  location = each.key
}

module "vnet" {
  for_each = var.regions

  source = "../../../dtos-devops-templates/infrastructure/modules/vnet"

  name                = module.regions_config[each.key].names.virtual-network
  resource_group_name = azurerm_resource_group.rg_vnet[each.key].name
  location            = each.key
  vnet_address_space  = each.value.address_space

  dns_servers = [data.terraform_remote_state.hub.outputs.private_dns_resolver_inbound_ips[each.key].private_dns_resolver_ip]

  tags = var.tags
}

/*--------------------------------------------------------------------------------------------------
  Create Subnets
--------------------------------------------------------------------------------------------------*/

locals {
  # Expand a flattened list of objects for all subnets (allows nested for loops)
  subnets_flatlist = flatten([
    for key, val in var.regions : [
      for subnet_key, subnet in val.subnets : merge({
        vnet_key         = key
        subnet_name      = coalesce(subnet.name, "${module.regions_config[key].names.subnet}-${subnet_key}")
        nsg_name         = "${module.regions_config[key].names.network-security-group}-${subnet_key}"
        nsg_rules        = lookup(var.network_security_group_rules, subnet_key, [])
        create_nsg       = coalesce(subnet.create_nsg, true)
        address_prefixes = cidrsubnet(val.address_space, subnet.cidr_newbits, subnet.cidr_offset)
      }, subnet) # include all the declared key/value pairs for a specific subnet
    ]
  ])
  # Project the above list into a map with unique keys for consumption in a for_each meta argument
  subnets_map = { for subnet in local.subnets_flatlist : subnet.subnet_name => subnet }
}

module "subnets" {
  for_each = local.subnets_map

  source = "../../../dtos-devops-templates/infrastructure/modules/subnet"

  name                              = each.value.subnet_name
  location                          = module.vnet[each.value.vnet_key].vnet.location
  network_security_group_name       = each.value.nsg_name
  network_security_group_nsg_rules  = each.value.nsg_rules
  create_nsg                        = each.value.create_nsg
  resource_group_name               = module.vnet[each.value.vnet_key].vnet.resource_group_name
  vnet_name                         = module.vnet[each.value.vnet_key].name
  address_prefixes                  = [each.value.address_prefixes]
  default_outbound_access_enabled   = true
  private_endpoint_network_policies = "Disabled" # Default as per compliance requirements

  delegation_name            = each.value.delegation_name != null ? each.value.delegation_name : ""
  service_delegation_name    = each.value.service_delegation_name != null ? each.value.service_delegation_name : ""
  service_delegation_actions = each.value.service_delegation_actions != null ? each.value.service_delegation_actions : []

  tags = var.tags
}



/*--------------------------------------------------------------------------------------------------
  Create peering
--------------------------------------------------------------------------------------------------*/

module "peering_spoke_hub" {
  # loop through regions and only create peering if connect_peering is set to true
  for_each = { for key, val in var.regions : key => val if val.connect_peering == true }

  source = "../../../dtos-devops-templates/infrastructure/modules/vnet-peering"

  name                = "${module.regions_config[each.key].names.virtual-network}-to-hub-peering"
  resource_group_name = azurerm_resource_group.rg_vnet[each.key].name
  vnet_name           = module.vnet[each.key].vnet.name
  remote_vnet_id      = data.terraform_remote_state.hub.outputs.vnets_hub[each.key].vnet.id

  allow_virtual_network_access = true
  allow_forwarded_traffic      = true
  allow_gateway_transit        = false

  use_remote_gateways = false
}

module "peering_hub_spoke" {
  for_each = { for key, val in var.regions : key => val if val.connect_peering == true }

  providers = {
    azurerm = azurerm.hub
  }

  source = "../../../dtos-devops-templates/infrastructure/modules/vnet-peering"

  name                = "hub-to-${module.regions_config[each.key].names.virtual-network}-peering"
  resource_group_name = data.terraform_remote_state.hub.outputs.vnets_hub[each.key].vnet.resource_group_name
  vnet_name           = data.terraform_remote_state.hub.outputs.vnets_hub[each.key].name
  remote_vnet_id      = module.vnet[each.key].vnet.id

  allow_virtual_network_access = true
  allow_forwarded_traffic      = true
  allow_gateway_transit        = false

  use_remote_gateways = false
}
