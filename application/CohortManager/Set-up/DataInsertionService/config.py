import os
import logging

"""Global config variables"""

LOCAL_ENV = os.getenv("LOCAL_ENVIRONMENT")

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

CAAS_COLUMN_MAPPINGS = {
    'primary_care_effective_from_date': 'Primary_Care_Provider_Business_Effective_From_Date',
    'current_posting_effective_from_date': 'Current_Posting_Business_Effective_From_Date',
    'other_given_name': 'Other_Given_Name(s)',
    'address_effective_from_date': 'Usual_Address_Business_Effective_From_Date',
    'reason_for_removal_effective_from_date': 'Reason_For_Removal_Business_Effective_From_Date',
    'home_telephone_number': 'Telephone_Number(Home)',
    'home_telephone_effective_from_date': 'Telephone_Number(Home)_Business_Effective_From_Date',
    'mobile_telephone_number': 'Telephone_Number(mobile)',
    'mobile_telephone_effective_from_date': 'Telephone_Number(Mobile)_Business_Effective_From_Date',
    'email_address': 'E-mail_Address(Home)',
    'email_address_effective_from_date': 'E-mail_Address(Home)_Business_Effective_From_Date',
    'date_of_death': 'Date_Of_Death(Formal)',
    'is_interpreter_required': 'Interpreter_Required'
}

BSS_COLUMN_MAPPINGS = {
    'removal_reason': 'REASON_FOR_REMOVAL',
    'removal_date': 'REASON_FOR_REMOVAL_FROM_DT',
    'gender_code': 'GENDER_CD'
}
