# output "log_analytics_workspace_id" {
#   value = module.log_analytics_workspace_audit.id
# }

# output "log_analytics_workspace_id" {
#   value = {
#     for ws in module.log_analytics_workspace_audit : ws => {
#       id = module.log_analytics_workspace_audit[ws].id
#     }
#   }
# }

output "log_analytics_workspace_id" {
  value = {
    for k, v in module.log_analytics_workspace_audit : k => {
      id = v.id
    }
  }
}
