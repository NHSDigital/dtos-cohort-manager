module "storage" {
  source = ".//modules/storage"

  name                = substr("${module.config.names.storage-account}${lower(var.storage_accounts.fnapp.name_suffix)}", 0, 24)
  resource_group_name = module.baseline.resource_group_names[var.storage_accounts.fnapp.resource_group_key]
  location            = module.baseline.resource_group_locations[var.storage_accounts.fnapp.resource_group_key]

  account_tier        = var.storage_accounts.fnapp.account_tier
  sa_replication_type = var.storage_accounts.fnapp.replication_type
  public_access       = var.storage_accounts.fnapp.public_network_access_enabled

  tags = var.tags

}
