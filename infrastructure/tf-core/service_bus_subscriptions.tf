module "service_bus_subscriptions" {
  for_each = local.service_bus_subscriber_map

  source = "../../../dtos-devops-templates/infrastructure/modules/service-bus-subscription"

  subscription_name         = each.value.subscriber
  max_delivery_count        = each.value.max_delivery_count
  topic_id                  = module.azure_service_bus["${each.value.service_bus_key}"].topic_ids[each.value.topic_name]
  namespace_name            = each.value.service_bus_key
  service_bus_namespace_id  = module.azure_service_bus["${each.value.service_bus_key}"].namespace_id
  function_app_principal_id = module.functionapp["${each.value.subscriber}-${each.value.region}"].function_app_sami_id
}

locals {

  service_bus_topic_list = flatten([
    for service_bus_key, service_bus_details in local.service_bus_map : [
      for topic_key, topic_details in service_bus_details.topics : merge(
        {
          service_bus_key     = service_bus_key
          service_bus_details = service_bus_details
          region              = service_bus_details.region
          topic_key           = topic_key
        },
        topic_details
      )
    ]
  ])

  service_bus_topic_map = {
    for topic in local.service_bus_topic_list : "${topic.service_bus_key}-${topic.topic_key}" => topic
  }

  service_bus_subscriber_list = flatten([
    for topic_key, topic_details in local.service_bus_topic_map : [
      for subscriber in topic_details.subscribers : {
        topic_key           = topic_key
        region              = topic_details.region
        subscriber          = subscriber
        service_bus_key     = topic_details.service_bus_key
        service_bus_details = topic_details.service_bus_details
        max_delivery_count  = topic_details.max_delivery_count
        topic_name          = topic_details.topic_key
      }
    ]
  ])

  service_bus_subscriber_map = {
    for subscriber in local.service_bus_subscriber_list : "${subscriber.topic_key}-${subscriber.subscriber}" => subscriber
  }
}
