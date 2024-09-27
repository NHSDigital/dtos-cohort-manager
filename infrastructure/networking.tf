resource "azurerm_resource_group" "rg_vnet" {
  for_each = var.regions

  name     = "${module.regions_config[each.key].names.resource-group}-networking"
  location = each.key
}

module "vnet" {
  for_each = var.regions

  # Source location updated to use the git:: prefix to avoid URL encoding issues - note // between the URL and the path is required
  source = "git::https://github.com/NHSDigital/dtos-devops-templates.git//infrastructure/modules/vnet?ref=2296f761f4edc3b413e2629c98309df9c6fa0849"

  name                = module.regions_config[each.key].names.virtual-network
  resource_group_name = azurerm_resource_group.rg_vnet[each.key].name
  location            = each.key
  vnet_address_space  = each.value.address_space

  tags = var.tags
}

/*--------------------------------------------------------------------------------------------------
  Create Subnets
--------------------------------------------------------------------------------------------------*/

module "hub-subnets" {
  for_each = local.subnets

  source = "git::https://github.com/NHSDigital/dtos-devops-templates.git//infrastructure/modules/subnet?ref=2296f761f4edc3b413e2629c98309df9c6fa0849"

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

  tags = var.tags
}

# Create flattened map of VNets and their subnets to use in the Subnets module above
locals {
  subnets_flatlist = flatten([for key, val in var.regions : [
    for subnet_key, subnet in val.subnets : {
      vnet_key         = key
      subnet_name      = coalesce(subnet.name, "${module.regions_config[key].names.subnet}-${subnet_key}")
      nsg_name         = "${module.regions_config[key].names.network-security-group}-${subnet_key}"
      nsg_rules        = lookup(var.network_security_group_rules, subnet_key, [])
      create_nsg       = coalesce(subnet.create_nsg, true)
      address_prefixes = cidrsubnet(val.address_space, subnet.cidr_newbits, subnet.cidr_offset)
    }
    ]
  ])

  subnets = { for subnet in local.subnets_flatlist : subnet.subnet_name => subnet }
}

/*--------------------------------------------------------------------------------------------------
  Create peering
--------------------------------------------------------------------------------------------------*/

module "peering_spoke_hub" {
  # loop through regions and only create peering if create_peering is set to true
  for_each = { for key, val in var.regions : key => val if val.create_peering == true }

  # Source location updated to use the git:: prefix to avoid URL encoding issues - note // between the URL and the path is required
  source = "git::https://github.com/NHSDigital/dtos-devops-templates.git//infrastructure/modules/vnet-peering?ref=2296f761f4edc3b413e2629c98309df9c6fa0849"

  name                = "${module.regions_config[each.key].names.virtual-network}-to-hub-peering"
  resource_group_name = azurerm_resource_group.rg_vnet[each.key].name
  vnet_name           = module.vnet[each.key].vnet.name
  remote_vnet_id      = data.terraform_remote_state.hub.outputs.vnets_hub[each.key].vnet.id

  allow_virtual_network_access = true
  allow_forwarded_traffic      = true
  allow_gateway_transit        = true

  use_remote_gateways = false
}

module "peering_hub_spoke" {
  for_each = var.regions

  providers = {
    azurerm = azurerm.devops
  }

  # Source location updated to use the git:: prefix to avoid URL encoding issues - note // between the URL and the path is required
  source = "git::https://github.com/NHSDigital/dtos-devops-templates.git//infrastructure/modules/vnet-peering?ref=2296f761f4edc3b413e2629c98309df9c6fa0849"

  name                = "hub-to-${module.regions_config[each.key].names.virtual-network}-peering"
  resource_group_name = data.terraform_remote_state.hub.outputs.vnets_hub[each.key].vnet.resource_group_name
  vnet_name           = data.terraform_remote_state.hub.outputs.vnets_hub[each.key].name
  remote_vnet_id      = module.vnet[each.key].vnet.id

  allow_virtual_network_access = true
  allow_forwarded_traffic      = true
  allow_gateway_transit        = true

  use_remote_gateways = false
}
