locals {
  # There are multiple App Service Plans and possibly multiple regions.
  # We cannot nest for loops inside a map, so first iterate all permutations of both as a list of objects...
  container_app_jobs_object_list = flatten([
    for region in keys(var.regions) : [
      for container_app_job, config in var.container_app_jobs.apps : merge(
        {
          region            = region            # 1st iterator
          container_app_job = container_app_job # 2nd iterator
        },
        config # the rest of the key/value pairs for a specific container_app_job
      )
    ]
  ])

  # ...then project the list of objects into a map with unique keys (combining the iterators), for consumption by a for_each meta argument
  container_app_jobs_map = {
    for object in local.container_app_jobs_object_list : "${object.container_app_job}-${object.region}" => object
  }
}

module "container-app-job" {
  for_each = local.container_app_jobs_map

  source = "../../../dtos-devops-templates/infrastructure/modules/container-app-job"

  name                = "ca-${lower(each.key)}"
  resource_group_name = azurerm_resource_group.core[each.value.region].name
  location            = each.value.region

  container_app_environment_id = module.container-app-environment["${each.value.container_app_environment_key}-${each.value.region}"].id
  user_assigned_identity_ids   = [module.managed_identity_sql_db_management[each.value.region].id]

  acr_login_server        = data.azurerm_container_registry.acr.login_server
  acr_managed_identity_id = each.value.container_registry_use_mi ? data.azurerm_user_assigned_identity.acr_mi.id : null
  docker_image            = "${data.azurerm_container_registry.acr.login_server}/${each.value.docker_image}:${each.value.docker_env_tag != "" ? each.value.docker_env_tag : var.docker_image_tag}"

  environment_variables = {
    "DtOsDatabaseConnectionString" = "Server=tcp:${module.regions_config[each.value.region].names.sql-server}.database.windows.net,1433;Initial Catalog=${var.sqlserver.dbs.cohman.db_name_suffix};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication='Active Directory Managed Identity';User ID=${module.managed_identity_sql_db_management[each.value.region].client_id};"
  }

  depends_on = [
    module.managed_identity_sql_db_management
  ]
}
