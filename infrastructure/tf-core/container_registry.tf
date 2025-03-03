module "acr" {

  # only create in regions where is_primary_region is true and only when acr map is not empty
  count = var.acr != null ? 1 : 0

  source = "../../../dtos-devops-templates/infrastructure/modules/container-registry"

  name                = module.regions_config[local.primary_region].names.azure-container-registry
  resource_group_name = azurerm_resource_group.core[local.primary_region].name
  location            = local.primary_region

  admin_enabled                 = var.acr.admin_enabled
  uai_name                      = var.acr.uai_name
  sku                           = var.acr.sku
  public_network_access_enabled = var.features.public_network_access_enabled

  # Private Endpoint Configuration if enabled
  private_endpoint_properties = var.features.private_endpoints_enabled ? {
    private_dns_zone_ids                 = [data.terraform_remote_state.hub.outputs.private_dns_zones["${local.primary_region}-container_registry"].id]
    private_endpoint_enabled             = var.features.private_endpoints_enabled
    private_endpoint_subnet_id           = module.subnets["${module.regions_config[local.primary_region].names.subnet}-pep"].id
    private_endpoint_resource_group_name = azurerm_resource_group.rg_private_endpoints[local.primary_region].name
    private_service_connection_is_manual = var.features.private_service_connection_is_manual
  } : null

  tags = var.tags
}
