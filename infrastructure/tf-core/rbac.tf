locals {
  rbac_roles_storage = {
    storage_account_contributor    = "Storage Account Contributor"
    storage_blob_data_owner        = "Storage Blob Data Owner"
    storage_queue_data_contributor = "Storage Queue Data Contributor"
  }

  wip_rbac_roles_storage = [ # this is needed to use the updated storage module - will refactor the other vars here later when function_app.tf is revised
    "Storage Account Contributor",
    "Storage Blob Data Owner",
    "Storage Queue Data Contributor"
  ]

  rbac_roles_database = {
    sql_contributor = "Contributor"
  }
}
