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
