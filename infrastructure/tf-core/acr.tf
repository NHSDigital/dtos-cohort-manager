module "acr" {

  # Cannot do this until we are ready to remove ACR from current Dev environment
  # as using the count will cause the ACR to be destroyed and recreated
  # count = var.features["acr_enabled"] ? 1 : 0

  source = ".//modules/azure-container-registry"

  names = module.config.names

  resource_group_name = module.baseline.resource_group_names[var.acr.resource_group_key]
  location            = module.baseline.resource_group_locations[var.acr.resource_group_key]

  sku           = var.acr.sku
  admin_enabled = var.acr.admin_enabled

  uai_name = var.acr.uai_name

  tags = var.tags

}
