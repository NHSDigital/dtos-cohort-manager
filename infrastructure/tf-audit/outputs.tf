output "log_analytics_workspace_id" {
  value = { for k, v in module.log_analytics_workspace_audit : k => v.id }
}
