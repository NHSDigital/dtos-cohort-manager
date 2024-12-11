import azure.functions as func
import azure.durable_functions as d_func
from config import logger
import time

compare_datasets_bp = d_func.Blueprint()

try:
    from database import setup_engine, get_table, insert_tables
    from discrepancy_flags import flag_discrepencies
    import numpy as np
    import pandas as pd
except Exception as e:
    logger.error(e)


@compare_datasets_bp.activity_trigger(input_name="params")
async def compare_datasets(params):
    # Set-up
    try:
        db_engine = setup_engine()
    except Exception as e:
        logger.error(e)

    # Reading data from DB
    caas_df = get_table("CAAS_PARTICIPANT", db_engine)
    bss_df = get_table("BSS_PARTICIPANT", db_engine)

    logger.info("pre-processing data")
    caas_df = preprocess_data(caas_df)
    bss_df = preprocess_data(bss_df)

    caas_only_df, bss_only_df = get_unique_rows(caas_df, bss_df)

    # Add discrepancy categories
    logger.info("Categorising discrepancies")
    start = time.time()
    caas_only_df = flag_discrepencies(caas_only_df)
    bss_only_df = flag_discrepencies(bss_only_df)
    end = time.time()
    logger.info(f"Discrepancies categorised in {((end - start) / 60):.2f} minutes")

    
    # Send to DB
    try:
        insert_tables(caas_only_df, "CAAS_ONLY_PARTICIPANT", db_engine)
        insert_tables(bss_only_df, "BSS_ONLY_PARTICIPANT", db_engine)
    except Exception as e:
        logger.error(e)

def preprocess_data(df):
    df.columns = df.columns.str.lower()
    df = df.fillna(np.nan).replace('', np.nan)

    return df

def get_unique_rows(caas_df, bss_df):
    logger.info("Merging datasets")
    start = time.time()
    merged_on_caas_df = pd.merge(caas_df, bss_df, on=["nhs_number", "date_of_birth"], how="left", suffixes=('', '_bss'), indicator=True)
    merged_on_bss_df = pd.merge(bss_df, caas_df, on=["nhs_number", "date_of_birth"], how="left", suffixes=('', '_caas'), indicator=True)

    # Drop rows and columns that are only in bss
    caas_only_df = merged_on_caas_df[merged_on_caas_df['_merge'] == 'left_only'].drop(columns=['_merge'])
    caas_only_df = caas_only_df.drop(columns=caas_only_df.filter(like='_bss').columns)

    bss_only_df = merged_on_bss_df[merged_on_bss_df['_merge'] == 'left_only'].drop(columns=['_merge'])
    bss_only_df = bss_only_df.drop(columns=bss_only_df.filter(like='_caas').columns)
    end = time.time()

    logger.info(f"Merge completed in {((end - start) / 60):.2f} minutes")

    return caas_only_df, bss_only_df