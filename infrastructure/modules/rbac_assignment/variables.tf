variable "scope" {
  description = "The scope at which the role assignment will be created."
  type        = string
}

variable "principal_id" {
  description = "The principal ID (e.g., user, group, or service principal) to which the role will be assigned."
  type        = string
}

variable "role_definition_name" {
  description = "The name of the role definition to assign."
  type        = string
}
