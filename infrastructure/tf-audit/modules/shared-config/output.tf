locals {
  names = {
    api-management              = lower("APIM-${var.env}-${var.application}-${var.location_map[var.location]}")
    app-insights                = upper("${var.env}-${var.location_map[var.location]}")
    app-service-plan            = lower("ASP-${var.application}-${var.env}-${var.location_map[var.location]}")
    app-service                 = lower("AS-${var.env}-${var.location_map[var.location]}-${var.application}")
    availability-set            = lower("AVS-${var.env}-${var.location_map[var.location]}-${var.application}")
    azure-container-registry    = lower("ACR${var.location_map[var.location]}${var.application}${var.env}")
    connection                  = upper("CON-${var.env}-${var.location_map[var.location]}-${var.application}")
    custom-image                = upper("IMAGE-${var.env}-${var.location_map[var.location]}")
    dns-zone                    = "${lower(var.application)}.${lower(var.env)}.net"
    docker-dtr                  = upper("DTR-${var.env}-${var.location_map[var.location]}-${var.application}")
    docker-manager              = upper("UCP-${var.env}-${var.location_map[var.location]}-${var.application}")
    docker-worker               = upper("LWK-${var.env}-${var.location_map[var.location]}-${var.application}")
    docker-worker-windows       = upper("WWK-${var.env}-${var.location_map[var.location]}-${var.application}")
    docker-worker-windows-nb    = upper("WWK${var.env}${var.location_map[var.location]}${var.application}")
    external-load-balancer      = upper("ELB-${var.env}-${var.location_map[var.location]}-${var.application}")
    event-grid-topic            = lower("EVGT-${var.env}-${var.location_map[var.location]}")
    function-app                = lower("${var.env}-${var.location_map[var.location]}")
    internal-load-balancer      = upper("ILB-${var.env}-${var.location_map[var.location]}-${var.application}")
    key-vault                   = upper("KV-${var.application}-${var.env}-${var.location_map[var.location]}")
    kubernetes-service          = lower("AKS-${var.env}-${var.location_map[var.location]}-${var.application}")
    load-balancer               = upper("LB-${var.env}-${var.location_map[var.location]}-${var.application}")
    local-network-gateway       = upper("LNG-${var.env}-${var.location_map[var.location]}-${var.application}")
    log-analytics-workspace     = upper("${var.env}-${var.location_map[var.location]}")
    logic-app                   = lower("LA-${var.env}-${var.location_map[var.location]}-${var.application}")
    network-interface           = upper("${var.env}-${var.location_map[var.location]}-${var.application}")
    network-security-group      = upper("NSG-${var.env}-${var.location_map[var.location]}-${var.application}")
    private-ssh-key             = lower("ssh-pri-${var.env}${var.location_map[var.location]}${var.application}")
    public-ip-address           = upper("PIP-${var.env}-${var.location_map[var.location]}-${var.application}")
    public-ip-dns               = lower("${var.env}${var.location_map[var.location]}${var.application}")
    public-ssh-key              = lower("ssh-pub-${var.env}${var.location_map[var.location]}${var.application}")
    redis-cache                 = lower("RC-${var.location_map[var.location]}-${var.env}-${var.application}")
    resource-group              = lower("RG-${var.application}-${var.env}-${var.location_map[var.location]}")
    resource-application        = upper("${var.env}-${var.location_map[var.location]}-${var.application}")
    route-table                 = upper("RT-${var.env}-${var.location_map[var.location]}-${var.application}")
    service-bus                 = lower("SB-${var.location_map[var.location]}-${var.env}-${var.application}")
    service-principal           = upper("SP-${var.env}-${var.application}")
    sql-server                  = lower("SQLSVR-${var.application}-${var.env}-${var.location_map[var.location]}")
    sql-server-db               = lower("SQLDB-${var.application}-${var.env}-${var.location_map[var.location]}")
    sql-server-managed-instance = lower("SQLMI-${var.env}-${var.location_map[var.location]}-${var.application}")
    stack-dns-suffix            = "${lower(var.env)}${lower(var.application)}"
    storage-account             = substr(lower("ST${var.application}${var.env}${var.location_map[var.location]}"), 0, 24)
    storage-alerts              = lower("STALERT${var.env}${var.location_map[var.location]}${var.application}")
    storage-boot-diags          = lower("STDIAG${var.env}${var.location_map[var.location]}${var.application}")
    storage-flow-logs           = lower("STFLOW${var.env}${var.location_map[var.location]}${var.application}")
    storage-shared-state        = lower("STSTATE${var.env}${var.location_map[var.location]}${var.application}")
    subnet                      = upper("SN-${var.env}-${var.location_map[var.location]}-${var.application}")
    virtual-machine             = upper("${var.env}-${var.application}")
    win-virtual-machine         = upper("${var.env}-${var.application}")
    virtual-network             = upper("VNET-${var.env}-${var.location_map[var.location]}-${var.application}")
    vnet-gateway                = upper("GWY-${var.env}-${var.location_map[var.location]}-${var.application}")
  }

}

output "names" {
  description = "Return list of calculated standard names for the deployment."
  value = {
    api-management              = local.names.api-management
    app-insights                = local.names.app-insights
    app-service-plan            = local.names.app-service-plan
    app-service                 = local.names.app-service
    availability-set            = local.names.availability-set
    azure-container-registry    = local.names.azure-container-registry
    connection                  = local.names.connection
    custom-image                = local.names.custom-image
    dns-zone                    = local.names.dns-zone
    docker-dtr                  = local.names.docker-dtr
    docker-manager              = local.names.docker-manager
    docker-worker               = local.names.docker-worker
    docker-worker-windows       = local.names.docker-worker-windows
    docker-worker-windows-nb    = local.names.docker-worker-windows-nb
    external-load-balancer      = local.names.external-load-balancer
    event-grid-topic            = local.names.event-grid-topic
    function-app                = local.names.function-app
    internal-load-balancer      = local.names.internal-load-balancer
    key-vault                   = local.names.key-vault
    kubernetes-service          = local.names.kubernetes-service
    load-balancer               = local.names.load-balancer
    local-network-gateway       = local.names.local-network-gateway
    log-analytics-workspace     = local.names.log-analytics-workspace
    logic-app                   = local.names.logic-app
    network-interface           = local.names.network-interface
    network-security-group      = local.names.network-security-group
    private-ssh-key             = local.names.private-ssh-key
    public-ip-address           = local.names.public-ip-address
    public-ip-dns               = local.names.public-ip-dns
    public-ssh-key              = local.names.public-ssh-key
    redis-cache                 = local.names.redis-cache
    resource-group              = local.names.resource-group
    resource-application        = local.names.resource-application
    route-table                 = local.names.route-table
    service-bus                 = local.names.service-bus
    service-principal           = local.names.service-principal
    sql-server                  = local.names.sql-server
    sql-server-db               = local.names.sql-server-db
    sql-server-managed-instance = local.names.sql-server-managed-instance
    stack-dns-suffix            = local.names.stack-dns-suffix
    storage-account             = local.names.storage-account
    storage-alerts              = local.names.storage-alerts
    storage-boot-diags          = local.names.storage-boot-diags
    storage-flow-logs           = local.names.storage-flow-logs
    storage-shared-state        = local.names.storage-shared-state
    subnet                      = local.names.subnet
    virtual-machine             = local.names.virtual-machine
    win-virtual-machine         = local.names.win-virtual-machine
    virtual-network             = local.names.virtual-network
    vnet-gateway                = local.names.vnet-gateway
  }
}