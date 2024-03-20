
locals {
  function_apps = { for fa in var.function_app.fa_config : fa.name_suffix => {
    name                = "${var.names.function-app}-${fa.name_suffix}"
    resource_group_name = var.resource_group_name
    location            = var.location
    service_plan_id     = var.asp_id

    storage_account_name       = var.sa_name
    storage_account_access_key = var.sa_prm_key

  } }
}


resource "azurerm_windows_function_app" "function" {
  for_each = local.function_apps

  name                = each.value.name
  resource_group_name = each.value.resource_group_name
  location            = each.value.location
  service_plan_id     = each.value.service_plan_id

  storage_account_name       = each.value.storage_account_name
  storage_account_access_key = each.value.storage_account_access_key

  site_config {}

  tags = var.tags

  lifecycle {
    ignore_changes = [tags]
  }

}
