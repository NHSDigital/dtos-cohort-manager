module "acr" {
  for_each = var.features.acr_enabled ? var.regions : {}

  source = "../../../dtos-devops-templates/infrastructure/modules/container-registry"

  name                = module.regions_config[each.key].names.azure-container-registry #-${lower(each.key.name_suffix)}"
  resource_group_name = azurerm_resource_group.core[each.key].name
  location            = each.key

  admin_enabled = var.container_registry.admin_enabled

  log_analytics_workspace_id                  = data.terraform_remote_state.audit.outputs.log_analytics_workspace_id[local.primary_region]
  monitor_diagnostic_setting_acr_enabled_logs = local.monitor_diagnostic_setting_acr_enabled_logs
  monitor_diagnostic_setting_acr_metrics      = local.monitor_diagnostic_setting_acr_metrics

  uai_name                      = var.container_registry.uai_name
  sku                           = var.container_registry.sku
  public_network_access_enabled = var.features.public_network_access_enabled

  # Private Endpoint Configuration if enabled
  private_endpoint_properties = var.features.private_endpoints_enabled ? {
    private_dns_zone_ids                 = [data.terraform_remote_state.hub.outputs.private_dns_zones["${each.key}-container_registry"].id]
    private_endpoint_enabled             = var.features.private_endpoints_enabled
    private_endpoint_subnet_id           = module.subnets["${module.regions_config[each.key].names.subnet}-pep"].id
    private_endpoint_resource_group_name = azurerm_resource_group.rg_private_endpoints[each.key].name
    private_service_connection_is_manual = var.features.private_service_connection_is_manual
  } : null

  tags = var.tags
}
