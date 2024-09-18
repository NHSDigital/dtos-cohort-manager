module "nsg" {
  for_each = var.regions

  source = ".//modules/network-security-group"

  name                = "${var.environment}-nsg-${each.value.caf_short_name}"
  resource_group_name = module.baseline.resource_group_names[var.network_security_group.resource_group_key]
  location            = each.key
  nsg_rules           = var.network_security_group.nsg_rules

  tags = var.tags
}
