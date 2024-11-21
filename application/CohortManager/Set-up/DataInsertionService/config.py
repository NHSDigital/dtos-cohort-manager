import os
import logging

"""Global config variables"""

ENV = os.getenv("ENVIRONMENT")

FILE_NAME = os.getenv("BLOB_NAME")

# TENANT_ID = os.getenv("TENANT_ID")
# CLIENT_ID = os.getenv("CLIENT_ID")
# CLIENT_SECRET = os.getenv("CLIENT_SECRET")

DB_NAME = os.getenv("DB_NAME")
TABLE_NAME = os.getenv("TABLE_NAME")

azure_storage_logger = logging.getLogger('azure.core.pipeline.policies.http_logging_policy').setLevel(logging.WARNING)

CAAS_SCHEMA = {
    "record_type": "string",
    "change_time_stamp": "Int64",
    "serial_change_number": "Int64",
    "nhs_number": "Int64",
    "superseded_by_nhs_number": "Int64",
    "primary_care_provider": "string",
    "primary_care_effective_from_date": "string",
    "current_posting": "string",
    "current_posting_effective_from_date": "string",
    "name_prefix": "string",
    "given_name": "string",
    "other_given_name": "string",
    "family_name": "string",
    "previous_family_name": "string",
    "date_of_birth": "string",
    "gender": "Int64",
    "address_line_1": "string",
    "address_line_2": "string",
    "address_line_3": "string",
    "address_line_4": "string",
    "address_line_5": "string",
    "postcode": "string",
    "paf_key": "string",
    "address_effective_from_date": "string",
    "reason_for_removal": "string",
    "reason_for_removal_effective_from_date": "string",
    "date_of_death": "string",
    "death_status": "Int32",
    "home_telephone_number": "string",
    "home_telephone_effective_from_date": "string",
    "mobile_telephone_number": "string",
    "mobile_telephone_effective_from_date": "string",
    "email_address": "string",
    "email_address_effective_from_date": "string",
    "preferred_language": "string",
    "is_interpreter_required": "int",
    "invalid_flag": "int",
    "eligibility": "int"
}