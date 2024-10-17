
### audit subscription

output "resource_group_rg_audit" {
  value = length(var.resource_groups) > 0 ? azurerm_resource_group.rg-audit : {}
}

output "resource_group_names_audit" {
  description = "The names of the created resource groups"
  value       = { for k, rg in azurerm_resource_group.rg-audit : k => rg.name }
}

output "resource_group_ids_audit" {
  description = "The IDs of the created resource groups"
  value       = { for k, rg in azurerm_resource_group.rg-audit : k => rg.id }
}

output "resource_group_locations_audit" {
  description = "The locations of the created resource groups"
  value       = { for k, rg in azurerm_resource_group.rg-audit : k => rg.location }
}
