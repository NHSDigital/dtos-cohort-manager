output "resource_groups" {
  value = length(var.resource_groups) > 0 ? azurerm_resource_group.rg : {}
}
