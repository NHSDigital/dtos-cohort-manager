module "service_bus_subscription" {
  for_each = local.service_bus_subscriptions_map

  source = "../../../../../dtos-devops-templates/infrastructure/modules/service-bus-subscription"

  subscription_name         = each.value.service_bus_subscription_key
  max_delivery_count        = 10
  topic_id                  = values(module.azure_service_bus["${each.value.namespace_name}-${each.value.region}"].topic_ids)[0]
  namespace_name            = "${each.value.namespace_name}-${each.value.region}"
  service_bus_namespace_id  = module.azure_service_bus["${each.value.namespace_name}-${each.value.region}"].namespace_id
  function_app_principal_id = module.functionapp["${each.value.subscriber_functionName}-${each.value.region}"].function_app_sami_id

}

locals {

  service_bus_subscriptions_object_list = flatten([
    for region in keys(var.regions) : [
      for service_bus_subscription_key, service_bus_subscription_details in var.service_bus_subscriptions.subscriber_config : merge(
        {
          region                       = region                       # 1st iterator
          service_bus_subscription_key = service_bus_subscription_key # 2nd iterator
        },
        service_bus_subscription_details # the rest of the key/value pairs for a specific service_bus
      )
    ]
  ])

  # ...then project the list of objects into a map with unique keys (combining the iterators), for consumption by a for_each meta argument
  service_bus_subscriptions_map = {
    for object in local.service_bus_subscriptions_object_list : "${object.service_bus_subscription_key}-${object.region}" => object
  }
}
