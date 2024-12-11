output "log_analytics_workspace_id" {
  value = { for k, v in module.log_analytics_workspace_audit : k => v.id }
}

output "storage_account_audit" {
  value = {
    for k, v in module.storage : k => {
      name                       = v.storage_account_name
      id                         = v.storage_account_id
      primary_blob_endpoint_name = v.primary_blob_endpoint_name
      containers                 = v.storage_containers
    }
  }
}

output "application_insights_id" {
  value = { for k, v in module.app_insights_audit : k => v.id }
}

output "subnet_pep_id" {
  value = { for k, v in module.subnets : local.subnets_map[k].vnet_key => v.id if k == "${module.regions_config[local.subnets_map[k].vnet_key].names.subnet}-pep" }
}

output "private_endpoint_rg_name" {
  value = var.features.private_endpoints_enabled ? { for k,v in azurerm_resource_group.rg_private_endpoints : k => v.name } : {}
}
