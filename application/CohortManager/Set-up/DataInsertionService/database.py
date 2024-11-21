import pandas as pd
from sqlalchemy import create_engine, URL
import logging
from config import *
import pyodbc

def setup_engine_local():
    """
    Sets up the local database engine, called if the ENV environment variable is set to 'Local'

    Returns:
        sqlalchemy.engine.base.Engine: The database engine
    """
    logging.info('Setting up local database engine')
    
    PASSWORD = os.getenv("PASSWORD")

    connection_string = f"mssql+pymssql://sa:{PASSWORD}@127.0.0.1/{DB_NAME}?charset=utf8"

    engine = create_engine(connection_string)

    return engine

def setup_engine_azure():
    """
    Sets up the Azure database engine, called if the ENV environment variable is set to 'Azure'

    Returns:
        sqlalchemy.engine.base.Engine: The database engine
    """
    logging.info('Setting up azure database engine')

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
