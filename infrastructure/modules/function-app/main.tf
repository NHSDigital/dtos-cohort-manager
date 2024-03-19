
resource "azurerm_windows_function_app" "function" {
  count = var.fnapp_count

  name                = "${var.name}-${format("%03d", count.index + 1)}"
  resource_group_name = var.resource_group_name
  location            = var.location
  service_plan_id     = var.asp_id

  storage_account_name       = var.sa_name
  storage_account_access_key = var.sa_prm_key

  site_config {

  }

  tags = var.tags

  lifecycle {
    ignore_changes = [tags]
  }

}
