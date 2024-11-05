module "firewall_policy_rule_collection_group" {
  for_each = var.routes

  source = "../../../dtos-devops-templates/infrastructure/modules/firewall-rule-collection-group"

  name               = "${module.regions_config[each.key].names.firewall}-audit-policy-rule-collection-group"
  firewall_policy_id = data.terraform_remote_state.hub.outputs.firewall_policy_id[each.key]
  priority           = 100

  network_rule_collection = [
    for rule_key, rule_val in each.value.network_rules : {
      name                  = rule_val.name
      priority              = rule_val.priority
      action                = rule_val.action
      rule_name             = rule_val.rule_name
      source_addresses      = rule_val.source_addresses == [] ? module.vnet[each.key].vnet.address_space : rule_val.source_addresses
      destination_addresses = rule_val.destination_addresses == [] ? data.azurerm_virtual_network.vnet_application[each.key].address_space : rule_val.destination_addresses
      protocols             = rule_val.protocols
      destination_ports     = rule_val.destination_ports
    }
  ]

}

module "route_table" {
  for_each = var.routes

  source = "../../../dtos-devops-templates/infrastructure/modules/route-table"

  name                = module.regions_config[each.key].names.route-table
  resource_group_name = azurerm_resource_group.rg_vnet[each.key].name
  location            = each.key

  bgp_route_propagation_enabled = each.value.bgp_route_propagation_enabled

  routes = [
    for route_key, route_val in each.value.route_table_routes : {
      name                   = route_val.name
      address_prefix         = route_val.address_prefix == "" ? data.azurerm_subnet.subnet_application_pep[each.key].address_prefixes[0] : route_val.address_prefix
      next_hop_type          = route_val.next_hop_type
      next_hop_in_ip_address = route_val.next_hop_in_ip_address == "" ? data.terraform_remote_state.hub.outputs.firewall_private_ip_addresses[each.key] : route_val.next_hop_in_ip_address
    }
  ]

  subnet_ids = [
    module.subnets["${module.regions_config[each.key].names.subnet}-pep"].id
  ]

  tags = var.tags
}
