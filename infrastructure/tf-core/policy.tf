locals {
  safe_policy_assignments = try(var.policy_assignments, {})
}

module "assign_policy" {
  source = "../../../dtos-devops-templates/infrastructure/modules/policy/policy-assignments"

  for_each = local.safe_policy_assignments

  name                 = each.value.name
  display_name         = each.value.display_name
  description          = each.value.description
  policy_definition_id = each.value.policy_definition_id
  scope                = "/subscriptions/${var.target_subscription_id}"

  parameters = {
    logAnalytics = {
      value = module.log_analytics_workspace_audit[local.primary_region].id
    }
  }
  location = "uksouth"
}

module "remediate_policy" {
  source = "../../../dtos-devops-templates/infrastructure/modules/policy/policy-remediation"

  for_each = module.assign_policy

  name                 = local.safe_policy_assignments[each.key].remediation_name
  display_name         = local.safe_policy_assignments[each.key].remediation_display_name
  scope                = each.value.scope
  policy_assignment_id = each.value.id

  depends_on = [module.assign_policy]
}
