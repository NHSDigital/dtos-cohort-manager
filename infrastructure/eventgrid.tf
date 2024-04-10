module "eventgrid" {
  source = ".//modules/event-grid"

  names = module.config.names

  resource_group_name = module.baseline.resource_group_names[var.event_grid.topic.resource_group_key]
  location            = module.baseline.resource_group_locations[var.event_grid.topic.resource_group_key]

  name_suffix = var.event_grid.topic.name_suffix

  tags = var.tags

}
