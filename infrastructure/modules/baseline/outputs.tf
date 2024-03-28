output "resource_groups" {
  value = length(var.resource_groups) > 0 ? azurerm_resource_group.rg : {}
}

output "resource_group_names" {
  description = "The names of the created resource groups"
  value       = { for k, rg in azurerm_resource_group.rg : k => rg.name }
}

output "resource_group_ids" {
  description = "The IDs of the created resource groups"
  value       = { for k, rg in azurerm_resource_group.rg : k => rg.id }
}

output "resource_group_locations" {
  description = "The locations of the created resource groups"
  value       = { for k, rg in azurerm_resource_group.rg : k => rg.location }
}

