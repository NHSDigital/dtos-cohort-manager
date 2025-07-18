locals {

  compliance_tags = {

    # ------------------------
    # FinOps
    # ------------------------
    TagVersion        = "1"                     # Tag version (e.g. 1, 2, 3)
    Programme-Service = "DToS/Breast Screening" # Programme name (custom value)
    Product-Project   = "Cohort Manager"        # Product name (custom value)
    Owner             = "<product owner>"       # Business owner (name or email)
    CostCentre        = "129106"                # Billing code (e.g. 128943)

    # ------------------------
    # SecOps
    # ------------------------
    data_classification = "3"              # Data level (1–5)
    DataType            = "PII"            # Data type (None, PCD, PII, Anonymised, UserAccount, Audit)
    ProjectType         = "In-development" # Project stage (PoC, Pilot, Production)
    PublicFacing        = "Y"              # Internet-facing (Y/N)

    # ------------------------
    # TechOps
    # ------------------------
    ServiceCategory = "Silver"      # Support tier (Bronze, Silver, Gold, Platinum)
    OnOffPattern    = "OfficeHours" # Uptime (AlwaysOn, OfficeHours, MF86, MF95, MF77)

    # ------------------------
    # DevOps
    # ------------------------
    ApplicationRole = "WebServer" # Resource role (Web, App, DB, WebServer, Firewall, LoadBalancer)

    Project = "Cohort-Manager"
  }

  primary_region = [for k, v in var.regions : k if v.is_primary_region][0]

  # Ensure that environment_tags is last so that it takes precedence over the compliance_tags
  merged_tags = merge(local.compliance_tags, var.tags)
}
