locals {
  rbac_roles_key_vault = []

  rbac_roles_storage = []

  rbac_roles_database = [
    "Contributor"
  ]

  rbac_roles_resource_group = [
    "Contributor",
    "Key Vault Certificates Officer",
    "Key Vault Certificate User",
    "Key Vault Crypto Officer",
    "Key Vault Crypto User",
    "Key Vault Secrets Officer",
    "Key Vault Secrets User",
    "Storage Account Contributor",
    "Storage Blob Data Owner",
    "Storage Queue Data Contributor"
  ]
}
