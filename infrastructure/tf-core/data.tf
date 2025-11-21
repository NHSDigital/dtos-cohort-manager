data "azurerm_client_config" "current" {}

data "terraform_remote_state" "hub" {
  backend = "azurerm"
  config = {
    subscription_id      = var.HUB_SUBSCRIPTION_ID
    storage_account_name = var.HUB_BACKEND_AZURE_STORAGE_ACCOUNT_NAME
    container_name       = var.HUB_BACKEND_AZURE_STORAGE_ACCOUNT_CONTAINER_NAME
    key                  = var.HUB_BACKEND_AZURE_STORAGE_ACCOUNT_KEY
    resource_group_name  = var.HUB_BACKEND_AZURE_RESOURCE_GROUP_NAME
  }
}

data "terraform_remote_state" "audit" {
  backend = "azurerm"
  config = {
    subscription_id      = var.HUB_SUBSCRIPTION_ID
    storage_account_name = var.AUDIT_BACKEND_AZURE_STORAGE_ACCOUNT_NAME
    container_name       = var.AUDIT_BACKEND_AZURE_STORAGE_ACCOUNT_CONTAINER_NAME
    key                  = var.AUDIT_BACKEND_AZURE_STORAGE_ACCOUNT_KEY
    resource_group_name  = var.AUDIT_BACKEND_AZURE_RESOURCE_GROUP_NAME
  }
}

data "azurerm_application_insights" "ai" {
  provider = azurerm.audit

  name                = data.terraform_remote_state.audit.outputs.application_insights.name
  resource_group_name = data.terraform_remote_state.audit.outputs.application_insights.resource_group_name
}

data "azurerm_virtual_network" "vnet_audit" {
  for_each = var.regions

  provider = azurerm.audit

  name                = module.regions_config[each.key].names.virtual-network
  resource_group_name = "${module.regions_config[each.key].names.resource-group}-audit-networking"
}

data "azurerm_subnet" "subnet_audit_pep" {
  for_each = var.regions

  provider = azurerm.audit

  name                 = "${module.regions_config[each.key].names.subnet}-pep"
  resource_group_name  = "${module.regions_config[each.key].names.resource-group}-audit-networking"
  virtual_network_name = module.regions_config[each.key].names.virtual-network
}

data "azuread_group" "sql_admin_group" {
  display_name = var.sqlserver.sql_admin_group_name
}

data "azurerm_container_registry" "acr" {
  provider = azurerm.hub

  name                = var.function_apps.acr_name
  resource_group_name = var.function_apps.acr_rg_name
}

data "azurerm_user_assigned_identity" "acr_mi" {
  provider = azurerm.hub

  name                = var.function_apps.acr_mi_name
  resource_group_name = var.function_apps.acr_rg_name
}

# data "azurerm_key_vault" "infra" {
#   for_each = var.key_vault != {} ? var.regions : {}

#   name                = module.regions_config[each.key].names.key-vault
#   resource_group_name = azurerm_resource_group.core[each.key].name
# }

# data "azurerm_key_vault_secret" "monitoring_email_address" {
#   for_each = var.features.alerts_enabled && var.key_vault != {} ? var.regions : {}

#   name         = "monitoring-email-address"
#   key_vault_id = data.azurerm_key_vault.infra[each.key].id
# }
