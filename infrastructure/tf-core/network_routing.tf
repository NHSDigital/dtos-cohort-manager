# module "firewall_policy_rule_collection_group" {
#   for_each = var.regions

#   source = "git::https://github.com/NHSDigital/dtos-devops-templates.git//infrastructure/modules/firewall-rule-collection-group?ref=feat/DTOSS-3407-Network-Routing-Config"

#   name               = module.config[each.key].names.firewall
#   firewall_policy_id = data.terraform_remote_state.hub.outputs.firewall_policy[each.key].firewall_policy.id
#   priority           = var.priority

# }

# module "route_table" {
#   for_each = local.route_table_routes

#   source = "git::https://github.com/NHSDigital/dtos-devops-templates.git//infrastructure/modules/route-table?ref=feat/DTOSS-3407-Network-Routing-Config"

#   name                = module.regions_config[each.key].names.route-table
#   resource_group_name = azurerm_resource_group.rg_vnet[each.key].name
#   location            = each.value.region

#   bgp_route_propagation_enabled = each.value.bgp_route_propagation_enabled

#   routes = [
#     for route_key, route_val in each.value : {
#       name                   = route_val.name
#       address_prefix         = route_val.address_prefix == "" ? data.azurerm_subnet.subnet_audit_pep[each.key].address_prefixes[0] : route_val.address_prefix
#       next_hop_type          = route_val.next_hop_type
#       next_hop_in_ip_address = route_val.next_hop_in_ip_address == "" ? data.terraform_remote_state.hub.outputs.firewall[each.key].ip_configuration[0].private_ip_address : route_val.next_hop_in_ip_address
#     }
#   ]

#   subnet_ids = [
#     module.subnets["${module.regions_config[each.value.region].names.subnet}-apps"].id
#   ]

#   tags = var.tags
# }

/* --------------------------------------------------------------------------------------------------
  Data lookups required to query other resource attributes
-------------------------------------------------------------------------------------------------- */

data "azurerm_subnet" "subnet_audit_pep" {
  for_each = var.regions

  provider = azurerm.audit

  name                 = "${module.regions_config[each.key].names.subnet}-pep"
  resource_group_name  = "${module.regions_config[each.key].names.resource-group}-audit-networking"
  virtual_network_name = module.regions_config[each.key].names.virtual-network
}

/* --------------------------------------------------------------------------------------------------
  Local variables used to create the routes and rules for the route table and firewall
-------------------------------------------------------------------------------------------------- */

locals {

  #   route_table_routes = {
  #     for region_key, region_val in var.regions :
  #     region_key => {
  #       for route in var.routes[region_key].route_table_routes :
  #       "${route.name}-${region_key}" => {
  #         name                          = route.name
  #         address_prefix                = route.address_prefix
  #         next_hop_type                 = route.next_hop_type
  #         next_hop_in_ip_address        = route.next_hop_in_ip_address
  #         bgp_route_propagation_enabled = route.bgp_route_propagation_enabled
  #         region                        = region_key
  #     } }
  #   }
  # }

  route_table_routes = flatten([
    for region_key, region_val in var.regions : [
      for route_key, route in routes[region_key] : {
        route_table_key               = "${route.name}-${region_key}"
        name                          = route.name
        address_prefix                = route.address_prefix
        next_hop_type                 = route.next_hop_type
        next_hop_in_ip_address        = route.next_hop_in_ip_address
        bgp_route_propagation_enabled = route.bgp_route_propagation_enabled
        region                        = region_key
      }
    ]
  ])
}

output "route_table" {
  value = local.route_table_routes
}
