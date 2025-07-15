# Project Infrastructure Modules Guidance

## Table of Contents

- [Role Based Access Control](#role-based-access-control)

---

## Role Based Access Control

The module `rbac.tf` centralises handling of global role based assignments for infrastructure resources in this project. While this is not the _only_ approach to handling roles and permissions, its aim is to help developers better manage access control.

There are two approaches to handling access control in this project's Terraform:

- The _current_ approach. This approach uses `rbac.tf` modules with each resource module located in `devops-templates/infrastructure/modules/xxx`
- An alternative approach which uses one or more _"global" role definitions_ that contain all necessary permissions for a given resource.

### Why custom role definitions?

The current security posture prevents the creation of _security groups_. Therefore a similar concept is found using _custom role definitions_. With this approach, we create a single definition that merges the default (least-privilege) permissions, and use the role together with a managed identity to assign to resources like `Key Vault`, `Storage Account`, `SQL Server` and `Function Apps`.

### High-level overview

#### Flow using Custom Roles

Is this supported? ✅
How? Do not specify any `rbac_roles` and ensure you set the `var.use_global_rbac_roles` to `true`.

```mermaid
flowchart LR
    markdown@{ shape: docs, label: "Terraform **plan** / **apply**" }
    rbac@{ shape: doc, label: "rbac.tf" }
    subgraph managed-identity-roles
        direction TB
        module@{ shape: docs, label: "**managed-identity-roles** module" }
        load_ids(["Load **resource IDs** in Terraform"])
        id(["`Create **managed identity**`"])
        defs(["`Create **role definitions**`"])
        assign(["`Assign roles to **managed identity** at **resource group** scope`"])
    end
    markdown --> rbac-->|references|managed-identity-roles
    module --> load_ids --> id --> defs --> assign
```

After the roles are assigned to the new "global" identity, the assignments in the Azure Portal will look similar to the following:

| Role                          | Resource Name                                 | Resource Type    | Principal                        |
|-------------------------------|-----------------------------------------------|------------------|----------------------------------|
| Contributor                   | sbmj-uks-manage-nems-subscription             | App Service      | mi-cohort-manager-global-uksouth |
| Contributor                   | sbmj-uks-delete-participant                   | App Service      | mi-cohort-manager-global-uksouth |
| Contributor                   | sbmj-uks-create-cohort-distribution           | App Service      | mi-cohort-manager-global-uksouth |
| Contributor                   | sbmj-uks-retrieve-participant-data            | App Service      | mi-cohort-manager-global-uksouth |
| Contributor                   | sbmj-uks-servicenow-cohort-lookup             | App Service      | mi-cohort-manager-global-uksouth |
| Contributor                   | sbmj-uks-file-validation                      | App Service      | mi-cohort-manager-global-uksouth |
| Contributor                   | sbmj-uks-get-validation-exceptions            | App Service      | mi-cohort-manager-global-uksouth |
| mi-global-role-keyvault-sbmj  | KV-COHMAN-SBMJ-UKS                            | Key Vault        | mi-cohort-manager-global-uksouth |
| Contributor                   | sbmj-uks-create-exception                     | App Service      | mi-cohort-manager-global-uksouth |
| Contributor                   | sbmj-uks-process-nems-update                  | App Service      | mi-cohort-manager-global-uksouth |
| mi-global-role-storage-sbmj   | stcohmansbmjuksfilexptns                      | Storage Account  | mi-cohort-manager-global-uksouth |
| Contributor                   | sbmj-uks-transform-data-service               | App Service      | mi-cohort-manager-global-uksouth |

#### Flow using current approach

Is this supported still? ✅
How? Specify the `rbac_roles` and ensure you set the `var.use_global_rbac_roles` to `false`.

```mermaid
flowchart LR
    subgraph functions
        direction LR

        fa_module@{ shape: docs, label: "**functionapp.tf** module" }
        fa_roles(["`Specify RBAC roles for
         **key_vault**, **sql** and **storage accounts**`"])

        fa_module --> fa_roles
    end

    subgraph storage
        direction LR

        st_module@{ shape: docs, label: "**storage.tf** module" }
        st_roles(["`Use defined RBAC roles for **storage**`"])

        st_module --> st_roles

    end

    subgraph web-app
        direction LR

        wa_module@{ shape: docs, label: "**web_app.tf** module" }
        wa_roles(["`Specify RBAC roles for
        **key vaults**, **sql** and **storage accounts**`"])

        wa_module --> wa_roles
    end

    subgraph keyvault
        direction LR

        kv_module@{ shape: docs, label: "**key_vault.tf** module" }
        kv_roles(["`Use defined RBAC roles for **storage**`"])

        kv_module --> kv_roles
    end

    subgraph templates
        fa_rbac@{ shape: docs, label: "function_app/**rbac.tf** module" }
        wa_rbac@{ shape: docs, label: "linux_web_app/**rbac.tf** module" }
        st_rbac@{ shape: docs, label: "storage/**rbac.tf** module"}
        kv_rbac@{ shape: docs, label: "key_vault/**rbac.tf** module"}
    end

    functions --> fa_rbac
    web-app --> wa_rbac
    storage --> st_rbac
    keyvault --> kv_rbac
```
