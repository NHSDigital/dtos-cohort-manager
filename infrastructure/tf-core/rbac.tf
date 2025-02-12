locals {
  rbac_roles_key_vault = [
    "Key Vault Certificate User",
    "Key Vault Crypto User",
    "Key Vault Secrets User"
  ]

  rbac_roles_key_vault_admin = [
    "Key Vault Certificates Officer",
    "Key Vault Crypto Officer",
    "Key Vault Secrets Officer"
  ]

  rbac_roles_resource_group = [
    "Contributor"
  ]

  rbac_roles_storage = [
    "Storage Account Contributor",
    "Storage Blob Data Owner",
    "Storage Queue Data Contributor"
  ]

  rbac_roles_database = [
    "Contributor"
  ]

}
