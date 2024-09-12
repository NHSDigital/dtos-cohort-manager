module "private_dns" {
  source = ".//modules/private-dns"

  resource_group_name = module.baseline.resource_group_names[var.private_dns.resource_group_key]
  location            = module.baseline.resource_group_locations[var.private_dns.resource_group_key]

  is_azure_sql_private_dns_zone_enabled = var.private_dns.is_azure_sql_private_dns_zone_enabled
  is_function_app_private_dns_zone_enabled = var.private_dns.is_function_app_private_dns_zone_enabled
  is_storage_private_dns_zone_enabled = var.private_dns.is_storage_private_dns_zone_enabled

  tags = var.tags
}
