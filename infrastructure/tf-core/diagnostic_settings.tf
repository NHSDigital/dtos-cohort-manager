locals {
  #APPSERVICEPLAN
  monitor_diagnostic_setting_appserviceplan_metrics = ["AllMetrics"]

  #FUNCTIONAPP
  monitor_diagnostic_setting_function_app_enabled_logs = ["FunctionAppLogs"]
  monitor_diagnostic_setting_function_app_metrics      = ["AllMetrics"]

  # KEYVAULT
  monitor_diagnostic_setting_keyvault_enabled_logs = ["VMProAuditEvent", "AzurePolicyEvaluationDetailstectionAlerts"]
  monitor_diagnostic_setting_keyvault_metrics      = ["AllMetrics"]

  #VNET
  monitor_diagnostic_setting_vnet_enabled_logs = ["VMProtectionAlerts"]
  monitor_diagnostic_setting_vnet_metrics      = ["AllMetrics"]
}
