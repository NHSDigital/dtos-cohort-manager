module "key_vault" {
  source = ".//modules/key-vault"

  names                    = module.config.names
  resource_group_name      = module.baseline.resource_groups[var.key_vault.resource_group_index].name
  location                 = module.baseline.resource_groups[var.key_vault.resource_group_index].location
  disk_encryption          = var.key_vault.disk_encryption
  soft_delete_retention    = var.key_vault.soft_del_ret_days
  purge_protection_enabled = var.key_vault.purge_prot
  sku_name                 = var.key_vault.sku_name

  tags = var.tags

}
