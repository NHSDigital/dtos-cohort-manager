
resource "azurerm_windows_function_app" "function" {
  for_each = var.function_app

  name                = "${var.names.function-app}-${lower(each.value.name_suffix)}"
  resource_group_name = var.resource_group_name
  location            = var.location
  service_plan_id     = var.asp_id

  storage_account_name       = var.sa_name
  storage_account_access_key = var.sa_prm_key

  site_config {}

  tags = var.tags

  lifecycle {
    ignore_changes = [tags]
  }

}
