module "storage" {
  for_each = var.storage_accounts

  source = ".//modules/storage"

  name                = substr("${module.regions_config[module.baseline.resource_group_locations[each.value.resource_group_key]].names.storage-account}${lower(each.value.name_suffix)}", 0, 24)
  resource_group_name = module.baseline.resource_group_names[each.value.resource_group_key]
  location            = module.baseline.resource_group_locations[each.value.resource_group_key]

  containers = each.value.containers

  account_replication_type      = each.value.replication_type
  account_tier                  = each.value.account_tier
  public_network_access_enabled = each.value.public_network_access_enabled

  tags = var.tags

}
