from sqlalchemy import create_engine, text
import logging
import os
from config import LOCAL_ENV
import pandas as pd

def setup_engine():
    """
    Sets up the local database engine, called if the ENV environment variable is set to 'Local'

    Returns:
        sqlalchemy.engine.base.Engine: The database engine
    """
    
    if LOCAL_ENV:
        logging.info('Setting up local database engine')
        PASSWORD = os.getenv("PASSWORD")
        DB_NAME = os.getenv("DB_NAME")
        connection_string = f"mssql+pymssql://sa:{PASSWORD}@127.0.0.1/{DB_NAME}?charset=utf8"
    else:
        logging.info('Setting up azure database engine')
        CONN_STR = os.getenv("DtOsDatabaseConnectionString")
        connection_string = (f"mssql+pyodbc:///?odbc_connect=Driver=ODBC+Driver+18+for+SQL+Server;" + CONN_STR)

    engine = create_engine(connection_string)

    return engine

def create_tables(engine):
    """Creates the CAAS_PARTICIPANT and BSS_PARTICIPANT tables"""

    with open('comparison_test_create_tables.sql') as file:
        sql = file.read()

    with engine.connect() as connection:
        connection.execute(text(sql))

    logging.info("Created comparison test tables")

def get_table(table, engine):
    df = pd.read_sql(f"SELECT * FROM {table}", engine)
    if df.shape[0] == 0: raise ValueError(f"No data returned from table {table}")

    return df
