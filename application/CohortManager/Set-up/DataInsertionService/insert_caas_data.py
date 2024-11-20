import azure.functions as func
import logging
from azure.identity import DefaultAzureCredential
from azure.storage.blob import BlobServiceClient
from config import *
import storage
import pandas as pd
import database as db

app = func.FunctionApp()

@app.route(route="InsertCaasData", auth_level=func.AuthLevel.Anonymous)
def InsertCaasData(req: func.HttpRequest) -> func.HttpResponse:
    """Inserts CaaS data from azure storage into the ???table name??? table. Triggered manually in azure"""

    logging.info('Data Loader triggered')

    if ENV == "local":
        file = storage.retrieve_file_azurite()
    else:
        file = storage.retrieve_file_azure()
    
    # Convert from parquet to CSV
    df = parquet_to_dataframe(file)

    # Load into database
    db.send_caas_data(df)

def parquet_to_dataframe(filename):
    """
    Converts the parquet file into a pandas dataframe object and cleans data for database

    Parameters:
        filename (string): the name of the file

    Returns:
        DataFrame: cleaned data in a pandas dataframe
    """

    # TODO: figure out whether to pass in file object or file name
    # TODO: Convert dates and bools

    schema = {
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
        "is_interpreter_required": "boolean",
        "invalid_flag": "boolean",
        "eligibility": "boolean"
    }

    df = pd.read_parquet(filename, engine='fastparquet').astype(schema)

    return df
