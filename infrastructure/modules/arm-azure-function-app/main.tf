data "azurerm_service_plan" "appserviceplan" {
  name                = var.appsvcplan_name
  resource_group_name = var.resource_group_name
}

data "azurerm_storage_account" "sa" {
  name                = var.sa_name
  resource_group_name = var.resource_group_name
}

resource "azurerm_windows_function_app" "function" {
  name                = var.name
  resource_group_name = var.resource_group_name
  location            = var.location
  service_plan_id     = data.azurerm_service_plan.appserviceplan.id

  storage_account_name       = data.azurerm_storage_account.sa.name
  storage_account_access_key = data.azurerm_storage_account.sa.primary_access_key

  site_config {

  }

  tags = var.tags

  lifecycle {
    ignore_changes = [tags]
  }

}
