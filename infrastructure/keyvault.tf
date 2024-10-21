module "key_vault" {
  for_each = {
    for key, value in var.regions : key => value
    if var.key_vault != {}
  }

  source = "git::https://github.com/NHSDigital/dtos-devops-templates.git//infrastructure/modules/key-vault?ref=3c65ba1334a4ecce82363b9448dabe375174221e"

  name                = module.regions_config[each.key].names.key-vault
  resource_group_name = module.baseline.resource_group_names[var.key_vault.resource_group_key]
  location            = each.key

  disk_encryption          = var.key_vault.disk_encryption
  soft_delete_retention    = var.key_vault.soft_del_ret_days
  purge_protection_enabled = var.key_vault.purge_prot
  sku_name                 = var.key_vault.sku_name

  # Private Endpoint Configuration if enabled
  private_endpoint_properties = var.features.private_endpoints_enabled ? {
    private_dns_zone_ids_keyvault        = [data.terraform_remote_state.hub.outputs.private_dns_zone_key_vault[each.key].private_dns_zone.id]
    private_endpoint_enabled             = var.features.private_endpoints_enabled
    private_endpoint_subnet_id           = module.subnets["${module.regions_config[each.key].names.subnet}-pep"].id
    private_endpoint_resource_group_name = azurerm_resource_group.rg_private_endpoints[each.key].name
    private_service_connection_is_manual = var.features.private_service_connection_is_manual
  } : null

  tags = var.tags
}
