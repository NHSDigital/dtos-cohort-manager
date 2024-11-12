output "log_analytics_workspace_id" {
  value = {
    for k, v in module.log_analytics_workspace_audit : k => {
      id = v.id
    }
  }
}

output "storage_account_name" {
  value = {
    for k, v in module.storage : k => {
      name = v.name
    }
  }
}