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
    metrics_categories                    = optional(string, "") #(string, "")
    log_categories = optional(map(object({
      enabled = bool
      })), {
      Administrative = { enabled = true },
      Security       = { enabled = true },
      ServiceHealth  = { enabled = true },
      Alert          = { enabled = true },
      Recommendation = { enabled = false },
      Policy         = { enabled = false },
      Autoscale      = { enabled = false },
      ResourceHealth = { enabled = true },
    })
  })
}


# log_categories                        = optional(string, ""),