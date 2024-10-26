module "firewall_policy_rule_collection_group" {
  for_each = var.routes

  source = "git::https://github.com/NHSDigital/dtos-devops-templates.git//infrastructure/modules/firewall-rule-collection-group?ref=feat/DTOSS-3407-Network-Routing-Config"

  name               = "${module.regions_config[each.key].names.firewall}-policy-rule-collection-group"
  firewall_policy_id = data.terraform_remote_state.hub.outputs.firewall_policy[each.key].firewall_policy_id
  priority           = each.value.network_rules[0].priority

  network_rule_collection = [
    for rule_key, rule_val in each.value.network_rules : {
      name                  = rule_val.name
      priority              = rule_val.priority
      action                = rule_val.action
      rule_name             = rule_val.rule_name
      source_addresses      = rule_val.source_addresses == [] ? module.vnet[each.key].address_space : rule_val.source_addresses
      destination_addresses = rule_val.destination_addresses == [] ? data.azurerm_virtual_network.vnet_audit[each.key].address_space : rule_val.destination_addresses
      protocols             = rule_val.protocols
      destination_ports     = rule_val.destination_ports
    }
  ]

}

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
      next_hop_in_ip_address = route_val.next_hop_in_ip_address == "" ? data.terraform_remote_state.hub.outputs.firewall_private_ip_addresses[each.key] : route_val.next_hop_in_ip_address
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

data "azurerm_virtual_network" "vnet_audit" {
  for_each = var.regions

  provider = azurerm.audit

  name                = module.regions_config[each.key].names.virtual-network
  resource_group_name = module.regions_config[each.key].names.resource-group
}

data "azurerm_subnet" "subnet_audit_pep" {
  for_each = var.regions

  provider = azurerm.audit

  name                 = "${module.regions_config[each.key].names.subnet}-pep"
  resource_group_name  = "${module.regions_config[each.key].names.resource-group}-audit-networking"
  virtual_network_name = module.regions_config[each.key].names.virtual-network
}

