locals {
  primary_region = [for k, v in var.regions : k if v.is_primary_region][0]

  activity_log_categories = [
    "Administrative",
    "Alert",
    "Autoscale",
    "Policy",
    "Recommendation",
    "ResourceHealth",
    "ServiceHealth"
  ]
}
