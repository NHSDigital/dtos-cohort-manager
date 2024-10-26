# module "firewall_policy_rule_collection_group" {
#   for_each = var.regions

#   source = "git::https://github.com/NHSDigital/dtos-devops-templates.git//infrastructure/modules/firewall-rule-collection-group?ref=feat/DTOSS-3407-Network-Routing-Config"

#   name               = module.config[each.key].names.firewall
#   firewall_policy_id = data.terraform_remote_state.hub.outputs.firewall_policy[each.key].firewall_policy.id
#   priority           = var.priority

# }

module "route_table" {
  for_each = var.routes

  source = "git::https://github.com/NHSDigital/dtos-devops-templates.git//infrastructure/modules/route-table?ref=feat/DTOSS-3407-Network-Routing-Config"

  name                = module.regions_config[each.key].names.route-table
  resource_group_name = azurerm_resource_group.rg_vnet[each.key].name
  location            = each.key

  bgp_route_propagation_enabled = each.value.bgp_route_propagation_enabled

  routes = [
    for route_key, route_val in each.value.route_table_routes : {
      name                   = route_val.name
      address_prefix         = route_val.address_prefix == "" ? data.azurerm_subnet.subnet_audit_pep[each.key].address_prefixes[0] : route_val.address_prefix
      next_hop_type          = route_val.next_hop_type
      next_hop_in_ip_address = route_val.next_hop_in_ip_address == "" ? data.terraform_remote_state.hub.outputs.firewall[each.key].ip_configuration[0].private_ip_address : route_val.next_hop_in_ip_address
    }
  ]

  subnet_ids = [
    module.subnets["${module.regions_config[each.key].names.subnet}-apps"].id
  ]

  tags = var.tags
}

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
  # Expand a flattened list of objects for all routes (allows nested for loops)
  route_table_routes = flatten([
    for region_key, region_val in var.routes : [
      for route in region_val.route_table_routes : {
        route_key              = "${route.name}-${region_key}"
        region                 = region_key
        name                   = route.name
        address_prefix         = route.address_prefix
        next_hop_type          = route.next_hop_type
        next_hop_in_ip_address = route.next_hop_in_ip_address
      }
    ]
  ])
  # Project the above list into a map with unique keys for consumption in a for_each meta argument
  route_table_routes_map = { for route in local.route_table_routes : route.route_key => route }
}

output "route_table" {
  value = local.route_table_routes
}

output "route_table_map" {
  value = local.route_table_routes_map
}
