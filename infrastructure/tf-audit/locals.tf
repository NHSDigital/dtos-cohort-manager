locals {
  primary_region = [for k, v in var.regions : k if v.is_primary_region][0]
  compliance_tags = {
    Project = "Cohort-Manager"

    # FIN OPS
    TagVersion="1"
    Programme-Service = "Screening/Cohort Management"
    Product-Project="Cohort Manager/DToS"
    Owner="<owner>"
    CostCentre="129106"

    #SEC OPS
    data_classification=1             #	depends on environment (1 for non-Prod, 5 for Prod)
    DataType="dev and integration"	  # depends on environment (dev and integration, anonymised - pre-prod, PII - prod)
    Environment="All environments (audit)"
    ProjectType=""
    PublicFacing="No"

    # TECH OPS
    ServiceCategory=""
    OnOffPattern="Always on"

    # DEV OPS
    ApplicationRole="WebApp"
    Name="Artifact Name"
  }

  merged_tags = merge(local.compliance_tags, var.tags)
}
