
resource "azurerm_log_analytics_workspace" "law_audit" {

  name                = var.name
  location            = var.location
  resource_group_name = var.audit_resource_group_name
  sku                 = var.law_sku
  retention_in_days   = var.retention_days

  tags = var.tags
}
