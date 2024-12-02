from config import CAAS_SCHEMA
import pandas as pd

def to_dataframe(stream):
    """
    Converts the parquet file into a pandas dataframe object using the schema

    Parameters:
        filename (string): the name of the file

    Returns:
        DataFrame: cleaned data in a pandas dataframe
    """

    df = pd.read_parquet(stream, engine='fastparquet').astype(CAAS_SCHEMA)
    df = format_dates(df)

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