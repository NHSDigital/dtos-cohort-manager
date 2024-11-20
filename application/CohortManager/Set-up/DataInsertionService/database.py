import pandas as pd
from sqlalchemy import create_engine
from config import *

DB_NAME = os.get_env("DB_NAME")

def send_caas_data(df):
    """Sends the dataframe data to the database"""
    
    if ENV == "Local":
        engine = setup_engine_local()
    else:
        engine = setup_engine_azure()

    df.to_sql('my_table', con=engine, if_exists='append', index=False, chunksize=1000)


def setup_engine_azure():
    """
    Sets up the Azure database engine, called if the ENV environment variable is set to 'Azure'

    Returns:
        sqlalchemy.engine.base.Engine: The database engine
    """

    # TODO: figure out what the server var should be
    connection_string = (
        f"Driver={{ODBC Driver 17 for SQL Server}};"
        f"Server={server};"
        f"Database={DB_NAME};"
        f"Authentication=ActiveDirectoryServicePrincipal;"
        f"UID={CLIENT_ID};"
        f"PWD={CLIENT_SECRET};"
        f"TenantId={TENANT_ID}"
    )

    engine = create_engine(f"mssql+pyodbc:///?odbc_connect={connection_string}")

    return engine

def setup_engine_local():
    """
    Sets up the local database engine, called if the ENV environment variable is set to 'Local'

    Returns:
        sqlalchemy.engine.base.Engine: The database engine
    """
    
    PASSWORD = os.get_env("PASSWORD")

    connection_string = (
        f"mssql+pyodbc://SA:{PASSWORD}@127.0.0.1/{DB_NAME}?driver=ODBC+Driver+17+for+SQL+Server"
    )

    engine = create_engine(connection_string)

    return engine