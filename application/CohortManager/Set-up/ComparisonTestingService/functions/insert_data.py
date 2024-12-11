import azure.functions as func
import azure.durable_functions as d_func
from config import logger

from io import BytesIO
from azure.core.exceptions import ResourceNotFoundError
from config import BSS_COLUMN_MAPPINGS, CAAS_COLUMN_MAPPINGS
import parquet
import storage
import pandas as pd
from database import create_tables, setup_engine, insert_tables

insert_data_bp = d_func.Blueprint()

@insert_data_bp.activity_trigger(input_name="params")
async def insert_data(params):
    filename = params["filename"]
    table_name = params["table_name"]

    if filename is None:
        logger.error("Filename not found in request")

    # Set up connections
    # try:
    logger.info("Establishing connections")
    blob_client = storage.get_blob_client(filename)
    db_engine = setup_engine()
    # except Exception as e:
    #     logger.error(e)

    # Read file
    # try:
    logger.info("Reading file")
    stream = blob_client.download_blob().readall()
    # except ResourceNotFoundError:
    #     logger.error("Blob does not exist")
    # except Exception as e:
    #     logger.error(e)
    
    # Convert file to DataFrame
    logger.info("Converting file to DataFrame")
    if filename.endswith(".csv"):
        df = pd.read_csv(BytesIO(stream))
        df.rename(columns=BSS_COLUMN_MAPPINGS, inplace=True)
    else:
        df = parquet.to_dataframe(BytesIO(stream))
        df.rename(columns=CAAS_COLUMN_MAPPINGS, inplace=True)

    num_duplicates = df.duplicated().sum()
    if num_duplicates != 0:
        logger.warning(f"{num_duplicates} have been found in the input file, this can cause unexpected outcomes in the comparison")

    # df['date_of_birth'] = pd.to_datetime(df['date_of_birth'])
    # df['date_of_death'] = pd.to_datetime(df['date_of_death'])
    # df['REASON_FOR_REMOVAL_BUSINESS_EFFECTIVE_FROM_DATE'] = pd.to_datetime(df['REASON_FOR_REMOVAL_BUSINESS_EFFECTIVE_FROM_DATE'])

    # Create tables & send data to the database
    # try:
    create_tables(db_engine)
    insert_tables(df, table_name, db_engine)
    # except Exception as e:
    #     logger.error(e)
