module "key_vault" {
  for_each = var.key_vault != {} ? var.regions : {}

  source = "../../../dtos-devops-templates/infrastructure/modules/key-vault"

  name                = module.regions_config[each.key].names.key-vault
  resource_group_name = azurerm_resource_group.core[each.key].name
  location            = each.key

  log_analytics_workspace_id                       = data.terraform_remote_state.audit.outputs.log_analytics_workspace_id[local.primary_region]
  monitor_diagnostic_setting_keyvault_enabled_logs = local.monitor_diagnostic_setting_keyvault_enabled_logs
  monitor_diagnostic_setting_keyvault_metrics      = local.monitor_diagnostic_setting_keyvault_metrics
  metric_enabled                                   = var.diagnostic_settings.metric_enabled

  disk_encryption          = var.key_vault.disk_encryption
  soft_delete_retention    = var.key_vault.soft_del_ret_days
  purge_protection_enabled = var.key_vault.purge_prot
  sku_name                 = var.key_vault.sku_name

  enable_rbac_authorization = true

  rbac_roles                = local.rbac_key_vault_roles

  # Private Endpoint Configuration if enabled
  private_endpoint_properties = var.features.private_endpoints_enabled ? {
    private_dns_zone_ids_keyvault        = [data.terraform_remote_state.hub.outputs.private_dns_zones["${each.key}-key_vault"].id]
    private_endpoint_enabled             = var.features.private_endpoints_enabled
    private_endpoint_subnet_id           = module.subnets["${module.regions_config[each.key].names.subnet}-pep"].id
    private_endpoint_resource_group_name = azurerm_resource_group.rg_private_endpoints[each.key].name
    private_service_connection_is_manual = var.features.private_service_connection_is_manual
  } : null

  depends_on = [
    module.peering_spoke_hub,
    module.peering_hub_spoke
  ]

  tags = var.tags
}

resource "azurerm_role_assignment" "global_cohort_mi_keyvault_role_assignments" {
  for_each = var.use_global_rbac_roles ? var.regions : {}

  # name = join("-", [
  #   each.value.id,
  #   local.get_role_local.get_definition_id[each.key],
  #   sha1(coalesce(var.rbac_principal_id, module.global_cohort_identity[each.value.region].principal_id))
  # ])

  principal_id = coalesce(
    # The user-supplied principal_id takes precedence
    var.rbac_principal_id,

    module.global_cohort_identity[each.key].principal_id
  )

  role_definition_id = module.global_cohort_identity_roles[each.key].keyvault_role_definition_id
  scope = module.key_vault[each.key].key_vault_id
}
