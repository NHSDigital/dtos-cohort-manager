module "nic_diagnostic_policy" {
  for_each = var.regions

  source = "../../../dtos-devops-templates/infrastructure/modules/policy/policy-definition"

  name         = "${module.regions_config[each.value.region].names.policy-definition}-nic-diag"
  display_name = "Deploy NIC Diagnostic Settings"

  parameters = {
    logAnalyticsWorkspaceId = data.terraform_remote_state.audit.outputs.log_analytics_workspace_id[local.primary_region]
  }

  policy_rule = {
    if = {
      field  = "type"
      equals = "Microsoft.Network/networkInterfaces"
    }
    then = {
      effect = "deployIfNotExists"
      details = {
        type = "Microsoft.Insights/diagnosticSettings"
        name = "setByPolicy"
        roleDefinitionIds = [
          "/providers/Microsoft.Authorization/roleDefinitions/b24988ac-6180-42a0-ab88-20f7382dd24c"
        ]
        existenceCondition = {
          field  = "Microsoft.Insights/diagnosticSettings/workspaceId"
          equals = "[parameters('logAnalyticsWorkspaceId')]"
        }
        deployment = {
          location = each.key
          properties = {
            mode = "incremental"
            template = {
              "$schema"      = "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"
              contentVersion = "1.0.0.0"
              parameters = {
                logAnalyticsWorkspaceId = { type = "string" }
                nicId                   = { type = "string" }
              }
              variables = {
                nicName = "[last(split(parameters('nicId'), '/'))]"
              }
              resources = [
                {
                  type       = "Microsoft.Network/networkInterfaces/providers/diagnosticSettings"
                  apiVersion = "2021-05-01-preview"
                  name       = "[concat(variables('nicName'), '/Microsoft.Insights/setByPolicy')]"
                  properties = {
                    workspaceId = "[parameters('logAnalyticsWorkspaceId')]"
                    metrics = [
                      {
                        category = "AllMetrics"
                        enabled  = true
                      }
                    ]
                  }
                }
              ]
            }
            parameters = {
              logAnalyticsWorkspaceId = {
                value = "[parameters('logAnalyticsWorkspaceId')]"
              }
              nicId = {
                value = "[field('id')]"
              }
            }
          }
        }
      }
    }
  }
}
