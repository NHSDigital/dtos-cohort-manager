output "resource_group_names" {
  description = "Names of Resource Groups"
  value       = module.baseline.resource_group_names
}

output "resource_group_locations" {
  description = "Locations of Resource Groups"
  value       = module.baseline.resource_group_locations
}
