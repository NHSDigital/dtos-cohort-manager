locals {
  rbac_roles_storage = {
    storage_account_contributor    = "Storage Account Contributor"
    storage_blob_data_owner        = "Storage Blob Data Owner"
    storage_queue_data_contributor = "Storage Queue Data Contributor"
  }

  rbac_roles_database = {
    sql_contributor = "Contributor"
  }
}
