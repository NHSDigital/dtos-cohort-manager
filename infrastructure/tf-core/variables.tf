variable "AUDIT_BACKEND_AZURE_STORAGE_ACCOUNT_NAME" {
  description = "The name of the Azure Storage Account for the audit backend"
  type        = string
}

variable "AUDIT_BACKEND_AZURE_STORAGE_ACCOUNT_CONTAINER_NAME" {
  description = "The name of the container in the Audit Azure Storage Account for the backend"
  type        = string
}

variable "AUDIT_BACKEND_AZURE_STORAGE_ACCOUNT_KEY" {
  description = "The name of the Statefile for the audit  resources"
  type        = string
}

variable "AUDIT_BACKEND_AZURE_RESOURCE_GROUP_NAME" {
  description = "The name of the audit resource group for the Azure Storage Account"
  type        = string
}

variable "AUDIT_SUBSCRIPTION_ID" {
  description = "ID of the Audit subscription to deploy infrastructure"
  type        = string
}

variable "HUB_BACKEND_AZURE_STORAGE_ACCOUNT_NAME" {
  description = "The name of the Azure Storage Account for the backend"
  type        = string
}

variable "HUB_BACKEND_AZURE_STORAGE_ACCOUNT_CONTAINER_NAME" {
  description = "The name of the container in the Azure Storage Account for the backend"
  type        = string
}

variable "HUB_BACKEND_AZURE_STORAGE_ACCOUNT_KEY" {
  description = "The name of the Statefile for the hub resources"
  type        = string
}

variable "HUB_BACKEND_AZURE_RESOURCE_GROUP_NAME" {
  description = "The name of the resource group for the Azure Storage Account"
  type        = string
}

variable "HUB_SUBSCRIPTION_ID" {
  description = "ID of the subscription hosting the DevOps resources"
  type        = string
}

variable "TARGET_SUBSCRIPTION_ID" {
  description = "ID of a subscription to deploy infrastructure"
  type        = string
}

variable "application" {
  description = "Project/Application code for deployment"
  type        = string
  default     = "DToS"
}

variable "application_full_name" {
  description = "Full name of the Project/Application code for deployment"
  type        = string
  default     = "DToS"
}

variable "docker_image_tag" {
  description = "Docker image tag to be used for application deployments"
  type        = string
  default     = ""
}

variable "environment" {
  description = "Environment code for deployments"
  type        = string
  default     = "DEV"
}

variable "features" {
  description = "Feature flags for the deployment"
  type        = map(bool)
}

variable "regions" {
  type = map(object({
    address_space     = optional(string)
    is_primary_region = bool
    connect_peering   = optional(bool, false)
    subnets = optional(map(object({
      cidr_newbits               = string
      cidr_offset                = string
      create_nsg                 = optional(bool, true) # defaults to true
      name                       = optional(string)     # Optional name override
      delegation_name            = optional(string)
      service_delegation_name    = optional(string)
      service_delegation_actions = optional(list(string))
    })))
  }))
}

### Cohort Manager specific variables ###
variable "app_service_plan" {
  description = "Configuration for the app service plan"
  type = object({
    os_type                  = optional(string, "Linux")
    vnet_integration_enabled = optional(bool, false)
    zone_balancing_enabled   = optional(bool, false)

    autoscale = object({
      scaling_rule = object({
        metric              = optional(string)
        capacity_min        = optional(string)
        capacity_max        = optional(string)
        capacity_def        = optional(string)
        time_grain          = optional(string)
        statistic           = optional(string)
        time_window         = optional(string)
        time_aggregation    = optional(string)
        inc_operator        = optional(string)
        inc_threshold       = optional(number)
        inc_scale_direction = optional(string)
        inc_scale_type      = optional(string)
        inc_scale_value     = optional(number)
        inc_scale_cooldown  = optional(string)
        dec_operator        = optional(string)
        dec_threshold       = optional(number)
        dec_scale_direction = optional(string)
        dec_scale_type      = optional(string)
        dec_scale_value     = optional(number)
        dec_scale_cooldown  = optional(string)
      })
    })

    instances = map(object({
      sku_name = optional(string, "P2v3")
      autoscale_override = optional(object({
        scaling_rule = object({
          metric              = optional(string)
          capacity_min        = optional(string)
          capacity_max        = optional(string)
          capacity_def        = optional(string)
          time_grain          = optional(string)
          statistic           = optional(string)
          time_window         = optional(string)
          time_aggregation    = optional(string)
          inc_operator        = optional(string)
          inc_threshold       = optional(number)
          inc_scale_direction = optional(string)
          inc_scale_type      = optional(string)
          inc_scale_value     = optional(number)
          inc_scale_cooldown  = optional(string)
          dec_operator        = optional(string)
          dec_threshold       = optional(number)
          dec_scale_direction = optional(string)
          dec_scale_type      = optional(string)
          dec_scale_value     = optional(number)
          dec_scale_cooldown  = optional(string)
        })
      }))
      wildcard_ssl_cert_key = optional(string, null)
    }))
  })
}

variable "container_app_environments" {
  description = "Configuration for the container app environments"
  default     = {}
  type = object({
    instances = optional(map(object({
      workload_profile = optional(object({
        name                  = optional(string)
        workload_profile_type = optional(string)
        minimum_count         = optional(number, 0)
        maximum_count         = optional(string, 1)
      }), {})
      zone_redundancy_enabled = optional(bool, false)
    })), {})
  })
}

variable "container_apps" {
  description = "Configuration for the container app jobs"
  default     = {}
  type = object({
    apps = optional(map(object({
      name_suffix                   = optional(string)
      container_app_environment_key = optional(string)
      docker_env_tag                = optional(string, "")
      docker_image                  = optional(string)
      is_web_app                    = optional(bool, false)
      container_registry_use_mi     = optional(bool, false)
    })), {})
  })
}

variable "container_app_jobs" {
  description = "Configuration for the container app jobs"
  default     = {}
  type = object({
    apps = optional(map(object({
      name_suffix                   = optional(string)
      container_app_environment_key = optional(string)
      docker_env_tag                = optional(string, "")
      docker_image                  = optional(string)
      container_registry_use_mi     = optional(bool, false)
    })), {})
  })
}

variable "diagnostic_settings" {
  description = "Configuration for the diagnostic settings"
  type = object({
    metric_enabled = optional(bool, false)
  })
}

variable "function_apps" {
  description = "Configuration for function apps"
  type = object({
    acr_mi_name                            = string
    acr_name                               = string
    acr_rg_name                            = string
    always_on                              = bool
    app_service_logs_disk_quota_mb         = optional(number)
    app_service_logs_retention_period_days = optional(number)
    cont_registry_use_mi                   = bool
    docker_CI_enable                       = string
    docker_env_tag                         = optional(string, "")
    docker_img_prefix                      = string
    enable_appsrv_storage                  = bool
    ftps_state                             = string
    health_check_path                      = optional(string, "")
    https_only                             = bool
    http2_enabled                          = optional(bool, false)
    pull_image_over_vnet                   = optional(bool, true)
    remote_debugging_enabled               = bool
    storage_uses_managed_identity          = bool
    worker_32bit                           = bool
    slots = optional(map(object({
      name         = string
      slot_enabled = optional(bool, false)
    })))
    fa_config = map(object({
      name_suffix                  = string
      function_endpoint_name       = string
      app_service_plan_key         = string
      storage_account_env_var_name = optional(string, "")
      storage_containers = optional(list(object
        ({
          env_var_name   = string
          container_name = string
      })), [])
      db_connection_string    = optional(string, "")
      service_bus_connections = optional(list(string), [])
      key_vault_url           = optional(string, "")
      app_urls = optional(list(object({
        env_var_name     = string
        function_app_key = string
        endpoint_name    = optional(string, "")
      })), [])
      env_vars_static = optional(map(string), {})
    }))
  })
}

variable "frontdoor_endpoint" {
  description = "Configuration for Front Door"
  type = map(object({
    origin = object({
      enabled    = optional(bool, true)
      priority   = optional(number, 1)   # 1–5
      webapp_key = string                # From var.linux_web_app.linux_web_app_config
      weight     = optional(number, 500) # 1–1000
    })

    origin_group = optional(object({
      health_probe = optional(object({
        interval_in_seconds = number # Required: 1–255
        path                = optional(string, "/")
        protocol            = optional(string, "Https")
        request_type        = optional(string, "HEAD")
      }))

      load_balancing = optional(object({
        additional_latency_in_milliseconds = optional(number, 50) # Optional: 0–1000
        sample_size                        = optional(number, 4)  # Optional: 0–255
        successful_samples_required        = optional(number, 3)  # Optional: 0–255
      }), {})

      session_affinity_enabled                                  = optional(bool, true)
      restore_traffic_time_to_healed_or_new_endpoint_in_minutes = optional(number)
    }), {})

    custom_domains = optional(map(object({
      dns_zone_name    = string
      dns_zone_rg_name = string
      host_name        = string

      tls = optional(object({
        certificate_type         = optional(string, "ManagedCertificate")
        cdn_frontdoor_secret_key = optional(string, null) # From var.projects[].frontdoor_profile.secrets in Hub
      }), {})
    })), {})

    route = optional(object({
      cache = optional(object({
        query_string_caching_behavior = optional(string, "IgnoreQueryString") # "IgnoreQueryString" etc.
        query_strings                 = optional(list(string))
        compression_enabled           = optional(bool, false)
        content_types_to_compress     = optional(list(string))
      }))

      cdn_frontdoor_origin_path = optional(string, null)
      enabled                   = optional(bool, true)
      forwarding_protocol       = optional(string, "MatchRequest") # "HttpOnly" | "HttpsOnly" | "MatchRequest"
      https_redirect_enabled    = optional(bool, false)
      link_to_default_domain    = optional(bool, false)
      patterns_to_match         = optional(list(string), ["/*"])
      supported_protocols       = optional(list(string), ["Https"])
    }), {})

    security_policies = optional(map(object({
      associated_domain_keys                = list(string)
      cdn_frontdoor_firewall_policy_name    = string
      cdn_frontdoor_firewall_policy_rg_name = string
    })), {})
  }))
}

variable "key_vault" {
  description = "Configuration for the key vault"
  type = object({
    disk_encryption   = optional(bool, true)
    soft_del_ret_days = optional(number, 7)
    purge_prot        = optional(bool, false)
    sku_name          = optional(string, "standard")
  })
}

variable "linux_web_app" {
  description = "Configuration for linux web apps"
  type = object({
    acr_mi_name                            = string
    acr_name                               = string
    acr_rg_name                            = string
    always_on                              = bool
    app_service_logs_disk_quota_mb         = optional(number)
    app_service_logs_retention_period_days = optional(number)
    cont_registry_use_mi                   = bool
    docker_env_tag                         = optional(string, "")
    docker_CI_enable                       = optional(string, "")
    docker_img_prefix                      = string
    enable_appsrv_storage                  = bool
    ftps_state                             = string
    health_check_path                      = optional(string, "")
    https_only                             = bool
    pull_image_over_vnet                   = optional(bool, true)
    remote_debugging_enabled               = optional(bool, false)
    storage_name                           = optional(string)
    storage_type                           = optional(string)
    share_name                             = optional(string)
    storage_account_access_key             = optional(string)
    storage_account_name                   = optional(string)
    worker_32bit                           = bool
    slots = optional(map(object({
      name         = string
      slot_enabled = optional(bool, false)
    })))
    linux_web_app_config = map(object({
      name_suffix          = string
      app_service_plan_key = string
      custom_domains       = optional(list(string), [])
      db_connection_string = optional(string, "")
      env_vars = optional(object({
        static         = optional(map(string), {})
        from_key_vault = optional(map(string), {})
        local_urls     = optional(map(string), {})
      }), {})
      key_vault_url                = optional(string, "")
      storage_account_env_var_name = optional(string, "")
      storage_containers = optional(list(object
        ({
          env_var_name   = string
          container_name = string
      })), [])
    }))
  })
}

variable "linux_web_app_slots" {
  description = "linux web app slots"
  type = list(object({
    linux_web_app_slots_name    = optional(string, "")
    linux_web_app_slots_enabled = optional(bool, false)
  }))
  default = []
}

variable "network_security_group_rules" {
  description = "The network security group rules."
  default     = {}
  type = map(list(object({
    name                       = string
    priority                   = number
    direction                  = string
    access                     = string
    protocol                   = string
    source_port_range          = string
    destination_port_range     = string
    source_address_prefix      = string
    destination_address_prefix = string
  })))
}

/*
  application_rule_collection = [
    {
      name      = "example-application-rule-collection-1"
      priority  = 600
      action    = "Allow"
      rule_name = "example-rule-1"
      protocols = [
        {
          type = "Http"
          port = 80
        },
        {
          type = "Https"
          port = 443
        }
      ]
      source_addresses  = ["0.0.0.0/0"]
      destination_fqdns = ["example.com"]
    },
*/

variable "routes" {
  description = "Routes configuration for different regions"
  type = map(object({
    bgp_route_propagation_enabled = optional(bool, false)
    firewall_policy_priority      = number
    application_rules = list(object({
      name      = optional(string)
      priority  = optional(number)
      action    = optional(string)
      rule_name = optional(string)
      protocols = list(object({
        type = optional(string)
        port = optional(number)
      }))
      source_addresses  = optional(list(string))
      destination_fqdns = list(string)
    }))
    nat_rules = list(object({
      name                = optional(string)
      priority            = optional(number)
      action              = optional(string)
      rule_name           = optional(string)
      protocols           = list(string)
      source_addresses    = list(string)
      destination_address = optional(string)
      destination_ports   = list(string)
      translated_address  = optional(string)
      translated_port     = optional(string)
    }))
    network_rules = list(object({
      name                  = optional(string)
      priority              = optional(number)
      action                = optional(string)
      rule_name             = optional(string)
      source_addresses      = optional(list(string))
      destination_addresses = optional(list(string))
      protocols             = optional(list(string))
      destination_ports     = optional(list(string))
    }))
    route_table_core = list(object({
      name                   = optional(string)
      address_prefix         = optional(string)
      next_hop_type          = optional(string)
      next_hop_in_ip_address = optional(string)
    }))
    route_table_audit = list(object({
      name                   = optional(string)
      address_prefix         = optional(string)
      next_hop_type          = optional(string)
      next_hop_in_ip_address = optional(string)
    }))
  }))
  default = {}
}

variable "sqlserver" {
  description = "Configuration for the Azure MSSQL server instance and a default database "
  type = object({

    sql_admin_group_name                 = optional(string)
    ad_auth_only                         = optional(bool)
    auditing_policy_retention_in_days    = optional(number)
    security_alert_policy_retention_days = optional(number)
    db_management_mi_name_prefix         = optional(string)

    # Server Instance
    server = optional(object({
      sqlversion                    = optional(string, "12.0")
      tlsversion                    = optional(number, 1.2)
      azure_services_access_enabled = optional(bool, true)
    }), {})

    # Database
    dbs = optional(map(object({
      db_name_suffix       = optional(string, "cohman")
      collation            = optional(string, "SQL_Latin1_General_CP1_CI_AS")
      licence_type         = optional(string, "LicenseIncluded")
      max_gb               = optional(number, 5)
      read_scale           = optional(bool, false)
      sku                  = optional(string, "S0")
      storage_account_type = optional(string, "Local")
      zone_redundant       = optional(bool, false)

      short_term_retention_policy = optional(number, null)
      long_term_retention_policy = optional(object({
        weekly_retention  = optional(string, null)
        monthly_retention = optional(string, null)
        yearly_retention  = optional(string, null)
        week_of_year      = optional(number, null)
      }), {})
    })), {})

    # FW Rules
    fw_rules = optional(map(object({
      fw_rule_name = string
      start_ip     = string
      end_ip       = string
    })), {})
  })
}

variable "service_bus" {
  description = "Configuration for Service Bus namespaces and their topics"
  default     = {}
  type = map(object({
    capacity         = number
    sku_tier         = string
    max_payload_size = string
    topics = map(object({
      auto_delete_on_idle                     = optional(string, "P10675199DT2H48M5.4775807S")
      batched_operations_enabled              = optional(bool, false)
      default_message_ttl                     = optional(string, "P10675199DT2H48M5.4775807S")
      duplicate_detection_history_time_window = optional(string)
      partitioning_enabled                    = optional(bool, false)
      max_message_size_in_kilobytes           = optional(number, 1024)
      max_size_in_megabytes                   = optional(number, 5120)
      max_delivery_count                      = optional(number, 10) # Note this actually belongs to the subscription, but is included here for convenience
      requires_duplicate_detection            = optional(bool, false)
      support_ordering                        = optional(bool)
      status                                  = optional(string, "Active")
      subscribers                             = optional(list(string), []) # List of function apps that will subscribe to this topic
    }))
  }))
}

variable "storage_accounts" {
  description = "Configuration for the Storage Account, currently used for Function Apps"
  type = map(object({
    name_suffix                             = string
    account_tier                            = optional(string, "Standard")
    blob_properties_delete_retention_policy = optional(number, 7)
    blob_properties_versioning_enabled      = optional(bool, false)
    replication_type                        = optional(string, "LRS")
    public_network_access_enabled           = optional(bool, false)
    containers = optional(map(object({
      container_name        = string
      container_access_type = optional(string, "private")
    })), {})
  }))
}

variable "tags" {
  description = "Default tags to be applied to resources"
  type        = map(string)
  default     = {}
}

variable "function_app_slots" {
  description = "function app slots"
  type = list(object({
    function_app_slots_name   = optional(string, "staging")
    function_app_slot_enabled = optional(bool, false)
  }))
}
