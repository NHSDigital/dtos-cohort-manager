variable "name" {
  type = string
}

variable "resource_group_name" {
  type        = string
  description = "The name of the resource group in which to create the SQL Server. Changing this forces a new resource to be created."
}

variable "location" {
  type        = string
  description = "The region where the Diagnostic Setting is created."
}

variable "target_resource_id" {
  type        = string
  description = "The resource where the Diagnostic Setting is created."
} 


variable "diagnostic_setting_properties" {
  description = "Consolidated properties for the Diagnostic Setting."
  type = object({
    diagnostic_settings_globally_enabled  = optional(bool, false),
    diagnostic_setting_audit_logs_enabled = optional(bool, false),
    log_analytics_workspace_id            = optional(string, ""),
    log_categories                        = optional(string, ""),
    #   catergory  = optional(string, "")
    # })),
    # logs_retention_policy      = optional(bool, false),
    # logs_retention_days        = optional(number, 0),
    metrics_categories = optional(string, "") #(string, ""),
    # metrics_retention_policy   = optional(bool, false),
    # metrics_retention_days     = optional(string, "")

  })
}