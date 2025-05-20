locals {
  # There are multiple App Service Plans and possibly multiple regions.
  # We cannot nest for loops inside a map, so first iterate all permutations of both as a list of objects...
  container_apps_object_list = flatten([
    for region in keys(var.regions) : [
      for container_app, config in var.container_apps.instances : merge(
        {
          region        = region        # 1st iterator
          container_app = container_app # 2nd iterator
        },
        config # the rest of the key/value pairs for a specific container_app
      )
    ]
  ])

  # ...then project the list of objects into a map with unique keys (combining the iterators), for consumption by a for_each meta argument
  container_apps_map = {
    for object in local.container_apps_object_list : "${object.container_app}-${object.region}" => object
  }
}


module "container-app-worker" {
  for_each = local.container_apps_map

  source = "../../../modules/dtos-devops-templates/infrastructure/modules/container-app"

  name                         = "${module.regions_config[each.value.region].names.container-app}-${lower(each.value.name_suffix)}"
  resource_group_name          = azurerm_resource_group.core[each.value.region].name
  container_app_environment_id = module.container-app-environment[each.value.container_app_environment_key].id
  docker_image                 = "${data.azurerm_container_registry.acr.login_server}/${each.value.docker_image}:${each.value.docker_env_tag}"
  environment_variables = {
    "DtOsDatabaseConnectionString" = "Server=${module.regions_config[region].names.sql-server}.database.windows.net; Authentication=Active Directory Managed Identity; Database=${var.sqlserver.dbs.cohman.db_name_suffix}; ApplicationIntent=ReadWrite; Pooling=true; Connection Timeout=30; Max Pool Size=300;"
    "SQL_IDENTITY_CLIENT_ID"       = data.azurerm_user_assigned_identity.db-management[each.value.region].client_id
  }
  is_web_app = each.value.is_web_app
}
