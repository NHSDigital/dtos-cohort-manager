module "storage" {
  source = ".//modules/storage"

  names = module.config.names

  resource_group_name = module.baseline.resource_group_names[var.storage_accounts.resource_group_key]
  location            = module.baseline.resource_group_locations[var.storage_accounts.resource_group_key]

  storage_accounts = var.storage_accounts.sa_config

  tags = var.tags

}
