data "azurerm_client_config" "current" {}

data "terraform_remote_state" "hub" {
  backend = "azurerm"
  config = {
    subscription_id      = var.HUB_SUBSCRIPTION_ID
    storage_account_name = var.HUB_BACKEND_AZURE_STORAGE_ACCOUNT_NAME
    container_name       = var.HUB_BACKEND_AZURE_STORAGE_ACCOUNT_CONTAINER_NAME
    key                  = var.HUB_BACKEND_AZURE_STORAGE_KEY
    resource_group_name  = var.HUB_BACKEND_AZURE_RESOURCE_GROUP_NAME
  }
}

data "azurerm_virtual_network" "vnet_application" {
  for_each = length(var.routes) > 0 ? var.regions : {}

  provider = azurerm.application

  name                = module.regions_config[each.key].names.virtual-network
  resource_group_name = "${module.regions_config[each.key].names.resource-group}-networking"
}

data "azurerm_subnet" "subnet_application_pep" {
  for_each = length(var.routes) > 0 ? var.regions : {}

  provider = azurerm.application

  name                 = "${module.regions_config[each.key].names.subnet}-pep"
  resource_group_name  = "${module.regions_config[each.key].names.resource-group}-networking"
  virtual_network_name = module.regions_config[each.key].names.virtual-network
}
