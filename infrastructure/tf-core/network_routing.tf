module "firewall_policy_rule_collection_group" {
  for_each = var.routes

  source = "../../../dtos-devops-templates/infrastructure/modules/firewall-rule-collection-group"

  name               = "${module.regions_config[each.key].names.firewall}-policy-rule-collection-group"
  firewall_policy_id = data.terraform_remote_state.hub.outputs.firewall_policy_id[each.key]
  priority           = each.value.firewall_policy_priority

  network_rule_collection = [
    for rule_key, rule_val in each.value.network_rules : {
      name                  = rule_val.name
      priority              = rule_val.priority
      action                = rule_val.action
      rule_name             = rule_val.rule_name
      source_addresses      = rule_val.source_addresses
      destination_addresses = rule_val.destination_addresses
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
    for route_key, route_val in each.value.route_table_routes_to_audit : {
      name                   = route_val.name
      address_prefix         = route_val.address_prefix
      next_hop_type          = route_val.next_hop_type
      next_hop_in_ip_address = route_val.next_hop_in_ip_address == "" ? data.terraform_remote_state.hub.outputs.firewall_private_ip_addresses[each.key] : route_val.next_hop_in_ip_address
    }
  ]

  subnet_ids = [
    module.subnets["${module.regions_config[each.key].names.subnet}-apps"].id,
    module.subnets["${module.regions_config[each.key].names.subnet}-pep"].id
  ]

  tags = var.tags
}

module "route_table_audit" {
  for_each = var.routes

  providers = {
    azurerm = azurerm.audit
  }

  source = "../../../dtos-devops-templates/infrastructure/modules/route-table"

  name                = module.regions_config[each.key].names.route-table
  resource_group_name = "${module.regions_config[each.key].names.resource-group}-audit-networking"
  location            = each.key

  bgp_route_propagation_enabled = each.value.bgp_route_propagation_enabled

  routes = [
    for route_key, route_val in each.value.route_table_routes_from_audit : {
      name                   = route_val.name
      address_prefix         = route_val.address_prefix
      next_hop_type          = route_val.next_hop_type
      next_hop_in_ip_address = route_val.next_hop_in_ip_address == "" ? data.terraform_remote_state.hub.outputs.firewall_private_ip_addresses[each.key] : route_val.next_hop_in_ip_address
    }
  ]

  subnet_ids = [
    data.azurerm_subnet.subnet_audit_pep[each.key].id
  ]

  tags = var.tags
}
