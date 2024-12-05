from sqlalchemy import create_engine, text
import os
from config import LOCAL_ENV, logger
import pandas as pd
import time
import pyodbc

def setup_engine():
    """
    Sets up the local database engine, called if the ENV environment variable is set to 'Local'

    Returns:
        sqlalchemy.engine.base.Engine: The database engine
    """

    if LOCAL_ENV:
        logger.info('Setting up local database engine')
        PASSWORD = os.getenv("PASSWORD")
        DB_NAME = os.getenv("DB_NAME")
        connection_string = f"mssql+pyodbc://SA:{PASSWORD}@127.0.0.1:1433/{DB_NAME}?driver=ODBC+Driver+18+for+SQL+Server&TrustServerCertificate=yes"
    else:
        logger.info('Setting up azure database engine')
        CONN_STR = os.getenv("DtOsDatabaseConnectionString")
        connection_string = (f"mssql+pyodbc:///?odbc_connect=Driver=ODBC+Driver+18+for+SQL+Server;" + CONN_STR)

    engine = create_engine(connection_string, fast_executemany=True)

    return engine

def create_tables(engine):
    """Creates the CAAS_PARTICIPANT and BSS_PARTICIPANT tables"""

    with open('comparison_test_create_tables.sql') as file:
        sql = file.read()

    with engine.connect() as connection:
        connection.execute(text(sql))

    logger.info("Created comparison test tables")

def get_table(table, engine):
    # CAAS_COLUMNS = ["NHS_NUMBER", "DATE_OF_BIRTH", "PRIMARY_CARE_PROVIDER",
    #                 "REASON_FOR_REMOVAL","REASON_FOR_REMOVAL_BUSINESS_EFFECTIVE_FROM_DATE",
    #                 "DATE_OF_DEATH","POSTCODE","GENDER"]
    
    # "ROW_INDEX", 

    start = time.time()
    if table == "CAAS_PARTICIPANT":
        df = pd.read_sql(f"SELECT NHS_NUMBER, DATE_OF_BIRTH, PRIMARY_CARE_PROVIDER, REASON_FOR_REMOVAL,REASON_FOR_REMOVAL_BUSINESS_EFFECTIVE_FROM_DATE, DATE_OF_DEATH, POSTCODE,GENDER FROM {table}", engine)
    else:
        df = pd.read_sql(f"SELECT * FROM {table}", engine)
    end = time.time()

    num_rows = df.shape[0]
    logger.info(f"Read {num_rows:,} rows from {table} table in {((end - start) / 60):.2f} minutes")

    if df.shape[0] == 0: raise ValueError(f"No data returned from table {table}")

    return df

def insert_tables(df, table, engine):
    logger.info(f"Inserting data into {table} table")

    num_rows = df.shape[0]

    start = time.time()
    df.to_sql(table, con=engine, if_exists='append', index=False, chunksize=50_000)
    end = time.time()

    logger.info(f"Inserted {num_rows:,} rows into {table} in {((end - start) / 60):.2f} minutes")
