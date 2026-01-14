module "azure_sql_server" {
  for_each = var.sqlserver != {} ? var.regions : {}

  source = "../../../dtos-devops-templates/infrastructure/modules/sql-server"

  # Azure SQL Server
  name                = module.regions_config[each.key].names.sql-server
  resource_group_name = azurerm_resource_group.core[each.key].name
  location            = each.key

  sqlversion = var.sqlserver.server.sqlversion
  tlsver     = var.sqlserver.server.tlsversion
  kv_id      = module.key_vault[each.key].key_vault_id


  # Diagnostic Settings
  log_analytics_workspace_id                         = data.terraform_remote_state.audit.outputs.log_analytics_workspace_id[local.primary_region]
  primary_blob_endpoint_name                         = data.terraform_remote_state.audit.outputs.storage_account_audit["sqllogs-${local.primary_region}"].primary_blob_endpoint_name
  storage_account_name                               = data.terraform_remote_state.audit.outputs.storage_account_audit["sqllogs-${local.primary_region}"].name
  storage_account_id                                 = data.terraform_remote_state.audit.outputs.storage_account_audit["sqllogs-${local.primary_region}"].id
  storage_container_id                               = data.terraform_remote_state.audit.outputs.storage_account_audit["sqllogs-${local.primary_region}"].containers["vulnerability-assessment"].id
  monitor_diagnostic_setting_database_enabled_logs   = local.monitor_diagnostic_setting_database_enabled_logs
  monitor_diagnostic_setting_database_metrics        = local.monitor_diagnostic_setting_database_metrics
  monitor_diagnostic_setting_sql_server_enabled_logs = local.monitor_diagnostic_setting_sql_server_enabled_logs
  monitor_diagnostic_setting_sql_server_metrics      = local.monitor_diagnostic_setting_sql_server_metrics

  sql_server_alert_policy_state = "Enabled"

  # Azure Monitor Alerting
  enable_alerting = var.features.alerts_enabled
  action_group_id = var.features.alerts_enabled ? module.monitor_action_group_performance.monitor_action_group.id : null

  sql_uai_name                         = null # not used - deprecate in module
  sql_admin_group_name                 = var.sqlserver.sql_admin_group_name
  sql_admin_object_id                  = data.azuread_group.sql_admin_group.object_id
  ad_auth_only                         = var.sqlserver.ad_auth_only
  security_alert_policy_retention_days = var.sqlserver.security_alert_policy_retention_days
  auditing_policy_retention_in_days    = var.sqlserver.auditing_policy_retention_in_days


  # Default database
  db_name_suffix       = var.sqlserver.dbs.cohman.db_name_suffix
  collation            = var.sqlserver.dbs.cohman.collation
  licence_type         = var.sqlserver.dbs.cohman.licence_type
  max_gb               = var.sqlserver.dbs.cohman.max_gb
  read_scale           = var.sqlserver.dbs.cohman.read_scale
  sku                  = var.sqlserver.dbs.cohman.sku
  storage_account_type = var.sqlserver.dbs.cohman.storage_account_type
  zone_redundant       = var.sqlserver.dbs.cohman.zone_redundant

  # FW Rules
  firewall_rules = var.sqlserver.fw_rules

  # Backup Retention Policies
  short_term_retention_policy = var.sqlserver.dbs.cohman.short_term_retention_policy
  long_term_retention_policy  = var.sqlserver.dbs.cohman.long_term_retention_policy

  # Private Endpoint Configuration if enabled
  private_endpoint_properties = var.features.private_endpoints_enabled ? {
    private_dns_zone_ids_sql             = [data.terraform_remote_state.hub.outputs.private_dns_zones["${each.key}-azure_sql"].id]
    private_endpoint_enabled             = var.features.private_endpoints_enabled
    private_endpoint_subnet_id           = module.subnets["${module.regions_config[each.key].names.subnet}-pep"].id
    private_endpoint_resource_group_name = azurerm_resource_group.rg_private_endpoints[each.key].name
    private_service_connection_is_manual = var.features.private_service_connection_is_manual
  } : null

  depends_on = [
    module.peering_spoke_hub,
    module.peering_hub_spoke
  ]

  tags = var.tags
}

# Create User Assigned Managed Identities for Azure SQL access by other resources

locals {
  managed_identities = flatten([
    for region, _ in var.regions : [
      for mi_name in var.sqlserver.user_assigned_identities : {
        region  = region
        mi_name = mi_name
      }
    ]
  ])

  managed_identities_map = {
    for object in local.managed_identities : "${object.mi_name}-${object.region}" => object
  }
}

module "user_assigned_managed_identity_sql" {
  for_each = local.managed_identities_map

  source = "../../../dtos-devops-templates/infrastructure/modules/managed-identity"

  uai_name            = "${module.regions_config[each.value.region].names.managed-identity}-${lower(each.value.mi_name)}"
  resource_group_name = azurerm_resource_group.core[each.value.region].name
  location            = each.value.region

  tags = var.tags
}

# Assign RBAC roles to the User Assigned Managed Identities for Azure SQL access by other resources
# DB-MANAGEMENT needs Contributor on the SQL Server to be able to run migrations
module "sql_db_management_rbac_assignment" {
  for_each = contains(var.sqlserver.user_assigned_identities, "db-management") && var.sqlserver != {} ? var.regions : {}

  source = "../../../dtos-devops-templates/infrastructure/modules/rbac-assignment"

  principal_id         = module.user_assigned_managed_identity_sql["db-management-${each.key}"].principal_id
  role_definition_name = "Contributor"
  scope                = module.azure_sql_server[each.key].sql_server_id

}

# DB-BACKUP needs SQL DB Contributor on the SQL Server to be able to read the database, and Storage Blob Data Contributor on the Storage Account to write the backups
module "sql_db_backup_rbac_assignment_sql_contributor" {
  for_each = contains(var.sqlserver.user_assigned_identities, "db-backup") && var.sqlserver != {} ? var.regions : {}

  source = "../../../dtos-devops-templates/infrastructure/modules/rbac-assignment"

  principal_id         = module.user_assigned_managed_identity_sql["db-backup-${each.key}"].principal_id
  role_definition_name = "SQL DB Contributor"
  scope                = module.azure_sql_server[each.key].sql_server_id
}

module "sql_db_backup_rbac_assignment_storage_contributor" {
  for_each = contains(var.sqlserver.user_assigned_identities, "db-backup") && var.sqlserver != {} ? var.regions : {}

  source = "../../../dtos-devops-templates/infrastructure/modules/rbac-assignment"

  principal_id         = module.user_assigned_managed_identity_sql["db-backup-${each.key}"].principal_id
  role_definition_name = "Storage Blob Data Contributor"
  scope                = data.terraform_remote_state.audit.outputs.storage_account_audit["sqlbackups-${local.primary_region}"].id
}


# DB-RESTORE needs SQL DB Contributor on the SQL Server to be able to write to the database, and Storage Blob Data Reader on the Storage Account to read the backups
module "sql_db_restore_rbac_assignment_sql_contributor" {
  for_each = contains(var.sqlserver.user_assigned_identities, "db-restore") && var.sqlserver != {} ? var.regions : {}

  source = "../../../dtos-devops-templates/infrastructure/modules/rbac-assignment"

  principal_id         = module.user_assigned_managed_identity_sql["db-restore-${each.key}"].principal_id
  role_definition_name = "SQL DB Contributor"
  scope                = module.azure_sql_server[each.key].sql_server_id
}

module "sql_db_restore_rbac_assignment_storage_reader" {
  for_each = contains(var.sqlserver.user_assigned_identities, "db-restore") && var.sqlserver != {} ? var.regions : {}

  source = "../../../dtos-devops-templates/infrastructure/modules/rbac-assignment"

  principal_id         = module.user_assigned_managed_identity_sql["db-restore-${each.key}"].principal_id
  role_definition_name = "Storage Blob Data Reader"
  scope                = data.terraform_remote_state.audit.outputs.storage_account_audit["sqlbackups-${local.primary_region}"].id
}
