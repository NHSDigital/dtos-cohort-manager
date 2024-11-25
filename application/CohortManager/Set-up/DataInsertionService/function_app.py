import azure.functions as func
import logging
from azure.identity import DefaultAzureCredential
from azure.storage.blob import BlobServiceClient
from azure.core.exceptions import ResourceNotFoundError
import os
from config import *
import storage
import pandas as pd
import database as db

app = func.FunctionApp()

@app.route(route="InsertCaasData")
def InsertCaasData(req: func.HttpRequest) -> func.HttpResponse:
    """Inserts CaaS data from azure storage into the CAAS_PARTICIPANT table. Triggered manually in azure"""

    logging.info('Insert CaaS Data Function triggered')
    logging.info(f'Environment is set to {ENV}')

    if ENV == "Local":
        blob_client = storage.get_blob_client_azurite(FILE_NAME)
        db_engine = db.setup_engine_local()
    else:
        blob_client = storage.get_blob_client_azure(FILE_NAME)
        db_engine = db.setup_engine_azure()

    try: 
        storage.read_file(blob_client, FILE_NAME)
    except ResourceNotFoundError:
        logging.error("Blob does not exist")
        return func.HttpResponse("Blob does not exist", status_code=400)
    
    df = parquet_to_dataframe(FILE_NAME)

    df.to_sql(TABLE_NAME, con=db_engine, if_exists='append', index=False, chunksize=1000)
    logging.info("Data inserted into the database")

    return func.HttpResponse("Success", status_code=200)

def parquet_to_dataframe(filename):
    """
    Converts the parquet file into a pandas dataframe object using the schema

    Parameters:
        filename (string): the name of the file

    Returns:
        DataFrame: cleaned data in a pandas dataframe
    """

    df = pd.read_parquet(filename, engine='fastparquet').astype(CAAS_SCHEMA)
    df = format_dates(df)
    df = rename_columns(df)

    return df

def format_dates(df):
    """Formats the date fileds (except change_time_stamp) to YYYY-MM-DD"""

    format = '%Y-%m-%d'

    df['current_posting_effective_from_date'] = pd.to_datetime(df['current_posting_effective_from_date']).dt.strftime(format)
    df['primary_care_effective_from_date'] = pd.to_datetime(df['primary_care_effective_from_date']).dt.strftime(format)
    df['date_of_birth'] = pd.to_datetime(df['date_of_birth']).dt.strftime(format)
    df['address_effective_from_date'] = pd.to_datetime(df['address_effective_from_date']).dt.strftime(format)
    df['reason_for_removal_effective_from_date'] = pd.to_datetime(df['reason_for_removal_effective_from_date']).dt.strftime(format)
    df['home_telephone_effective_from_date'] = pd.to_datetime(df['home_telephone_effective_from_date']).dt.strftime(format)
    df['mobile_telephone_effective_from_date'] = pd.to_datetime(df['mobile_telephone_effective_from_date']).dt.strftime(format)
    df['email_address_effective_from_date'] = pd.to_datetime(df['email_address_effective_from_date']).dt.strftime(format)

    return df

def rename_columns(df):
    """Rename columns according to database names"""

    COLUMN_NAME_MAPPINGS = {
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

    df.rename(columns=COLUMN_NAME_MAPPINGS, inplace=True)

    return df
