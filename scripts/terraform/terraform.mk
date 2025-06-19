# This file is for you! Edit it to implement your own Terraform make targets.

# ==============================================================================
# Custom implementation - implementation of a make target should not exceed 5 lines of effective code.
# In most cases there should be no need to modify the existing make targets.

terraform-init: # Initialise Terraform - optional: terraform_dir|dir=[path to a directory where the command will be executed, relative to the project's top-level directory, default is one of the module variables or the example directory, if not set], terraform_opts|opts=[options to pass to the Terraform init command, default is none/empty] @Development
	make _terraform cmd="init" \
		dir="${terraform_dir:-${dir}}" \
		opts="${terraform_opts:-${opts}}"

terraform-plan: # Plan Terraform changes - optional: terraform_dir|dir=[path to a directory where the command will be executed, relative to the project's top-level directory, default is one of the module variables or the example directory, if not set], terraform_opts|opts=[options to pass to the Terraform plan command, default is none/empty] @Development
	make _terraform cmd="plan" \
		dir="${terraform_dir:-${dir}}" \
		opts="${terraform_opts:-${opts}}"

terraform-apply: # Apply Terraform changes - optional: terraform_dir|dir=[path to a directory where the command will be executed, relative to the project's top-level directory, default is one of the module variables or the example directory, if not set], terraform_opts|opts=[options to pass to the Terraform apply command, default is none/empty] @Development
	make _terraform cmd="apply" \
		dir="${terraform_dir:-${dir}}" \
		opts="${terraform_opts:-${opts}}"

terraform-destroy: # Destroy Terraform resources - optional: terraform_dir|dir=[path to a directory where the command will be executed, relative to the project's top-level directory, default is one of the module variables or the example directory, if not set], terraform_opts|opts=[options to pass to the Terraform destroy command, default is none/empty] @Development
	make _terraform \
		cmd="destroy" \
		dir="${terraform_dir:-${dir}}" \
		opts="${terraform_opts:-${opts}}"

terraform-fmt: # Format Terraform files - optional: terraform_dir|dir=[path to a directory where the command will be executed, relative to the project's top-level directory, default is one of the module variables or the example directory, if not set], terraform_opts|opts=[options to pass to the Terraform fmt command, default is '-recursive'] @Quality
	make _terraform cmd="fmt" \
		dir="${terraform_dir:-${dir}}" \
		opts="${terraform_opts:-${opts}}"

terraform-validate: # Validate Terraform configuration - optional: terraform_dir|dir=[path to a directory where the command will be executed, relative to the project's top-level directory, default is one of the module variables or the example directory, if not set], terraform_opts|opts=[options to pass to the Terraform validate command, default is none/empty] @Quality
	make _terraform cmd="validate" \
		dir="${terraform_dir:-${dir}}" \
		opts="${terraform_opts:-${opts}}"

clean:: # Remove Terraform files (terraform) - optional: terraform_dir|dir=[path to a directory where the command will be executed, relative to the project's top-level directory, default is one of the module variables or the example directory, if not set] @Operations
	make _terraform cmd="clean" \
		dir="${terraform_dir:-${dir}}" \
		opts="${terraform_opts:-${opts}}"

_terraform: # Terraform command wrapper - mandatory: cmd=[command to execute]; optional: dir=[path to a directory where the command will be executed, relative to the project's top-level directory, default is one of the module variables or the example directory, if not set], opts=[options to pass to the Terraform command, default is none/empty]
	# 'TERRAFORM_STACK' is passed to the functions as environment variable
	@TERRAFORM_STACK="${TERRAFORM_STACK:-${terraform_stack:-${STACK:-${stack:-scripts/terraform/examples/terraform-state-aws-s3}}}}"; \
	dir="${dir:-$$TERRAFORM_STACK}"; \
	source scripts/terraform/terraform.lib.sh; \
	terraform-${cmd}

# ==============================================================================
# Quality checks - please DO NOT edit this section!

terraform-shellscript-lint: # Lint all Terraform module shell scripts @Quality
	for file in $$(find scripts/terraform -type f -name "*.sh"); do
		file=$${file} scripts/shellscript-linter.sh
	done

# ==============================================================================
# Module tests and examples - please DO NOT edit this section!

terraform-example-provision-aws-infrastructure: # Provision example of AWS infrastructure @ExamplesAndTests
	make terraform-init
	make terraform-plan opts="-out=terraform.tfplan"
	make terraform-apply opts="-auto-approve terraform.tfplan"

terraform-example-destroy-aws-infrastructure: # Destroy example of AWS infrastructure @ExamplesAndTests
	make terraform-destroy opts="-auto-approve"

terraform-example-clean: # Remove Terraform example files @ExamplesAndTests
	@dir="${dir:-${TERRAFORM_STACK}}"; \
	source scripts/terraform/terraform.lib.sh; \
	terraform-clean; \
	rm -f "$$dir/.terraform.lock.hcl"

# ==============================================================================
# Configuration - please DO NOT edit this section!

terraform-install: # Install Terraform @Installation
	make _install-dependency name="terraform"

# ==============================================================================

${VERBOSE}.SILENT: \
	_terraform \
	clean \
	terraform-apply \
	terraform-destroy \
	terraform-example-clean \
	terraform-example-destroy-aws-infrastructure \
	terraform-example-provision-aws-infrastructure \
	terraform-fmt \
	terraform-init \
	terraform-install \
	terraform-plan \
	terraform-shellscript-lint \
	terraform-validate \
