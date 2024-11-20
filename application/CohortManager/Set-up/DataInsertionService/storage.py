import os
from azure.identity import ClientSecretCredential
from azure.storage.blob import BlobServiceClient
from config import *

BLOB_NAME = os.getenv("BLOB_NAME")
BLOB_CONTAINER_NAME = os.getenv("BLOB_CONTAINER_NAME")

def read_file(blob_client, filename):
    """
    Reads the file from azurite/ azure storage

    Parameters:
        filename (string): the name of the file
        blob_client (azure.storage.blob.BlobClient): The blob client to read the file

    Returns:
        Something: The CSV/ Parquet file
    """

    # Open a local file to write the blob content
    with open(filename, "wb") as file:
        # Download the blob and write it to a file in chunks
        download_stream = blob_client.download_blob()
        for chunk in download_stream.chunks():
            file.write(chunk)

    return file

def retrieve_file_azurite(filename):
    """
    Sets up the blob client and reads the file from azurite

    Parameters:
        filename (string): the name of the file

    Returns:
        Something: The CSV/ Parquet file
    """

    AZURITE_CONNECTION_STRING = os.getenv("AZURITE_CONNECTION_STRING")

    blob_service_client = BlobServiceClient.from_connection_string(AZURITE_CONNECTION_STRING)
    blob_client = blob_service_client.get_blob_client(container=BLOB_CONTAINER_NAME, blob=BLOB_NAME)

    file = read_file(blob_client, filename)

    return file

def retrieve_file_azure(filename):
    """
    Sets up the blob client and reads the file from azure storage

    Parameters:
        filename (string): the name of the file

    Returns:
        Something: The CSV/ Parquet file
    """

    credential = ClientSecretCredential(tenant_id=TENANT_ID, client_id=CLIENT_ID, client_secret=CLIENT_SECRET)

    STORAGE_ACCOUNT_NAME = os.getenv("STORAGE_ACCOUNT_NAME")

    # Form the Blob Service Client URL
    blob_service_client_url = f"https://{STORAGE_ACCOUNT_NAME}.blob.core.windows.net"
    
    # Create the BlobServiceClient with Entra ID credentials
    blob_service_client = BlobServiceClient(account_url=blob_service_client_url, credential=credential)
    blob_client = blob_service_client.get_blob_client(container=BLOB_CONTAINER_NAME, blob=BLOB_NAME)

    file = read_file(blob_client, filename)

    return file