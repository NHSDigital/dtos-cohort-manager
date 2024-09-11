module "nsg" {
  source = ".//modules/network-security-group"

  name                = module.config.names.network-security-group
  resource_group_name = module.baseline.resource_group_names[var.network_security_group.resource_group_key]
  location            = module.baseline.resource_group_locations[var.network_security_group.resource_group_key]
  nsg_rules           = var.network_security_group.nsg_rules

  tags = var.tags
}
