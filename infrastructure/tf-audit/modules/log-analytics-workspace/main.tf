
resource "azurerm_log_analytics_workspace" "log_analytics_workspace" {

  name                = var.name
  location            = var.location
  resource_group_name = var.resource_group_name
  sku                 = var.law_sku
  retention_in_days   = var.retention_days

  tags = var.tags
}
