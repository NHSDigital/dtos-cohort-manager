import os
from azure.identity.aio import DefaultAzureCredential
from azure.storage.blob import BlobServiceClient
import logging
from config import *

BLOB_CONTAINER_NAME = os.getenv("BLOB_CONTAINER_NAME")

def read_file(blob_client, filename):
    """
    Reads the file from azurite/ azure storage and saves it to the filesystem

    Parameters:
        filename (string): the name of the file
        blob_client (azure.storage.blob.BlobClient): The blob client to read the file
    """
    logging.info("Reading file")

    # Open a local file to write the blob content
    with open(filename, "wb") as file:
        # Download the blob and write it to a file in chunks
        download_stream = blob_client.download_blob()
        for chunk in download_stream.chunks():
            file.write(chunk)

    logging.info("File saved")

def get_blob_client(filename):
    """
    Sets up the blob client

    Parameters:
        filename (string): the name of the blob

    Returns:
        BlobClient
    """

    # if LOCAL_ENV:
    CONNECTION_STRING = os.getenv("STORAGE_CONNECTION_STRING")

    blob_service_client = BlobServiceClient.from_connection_string(CONNECTION_STRING, logging=azure_storage_logger)
    # else:
    #     credential = DefaultAzureCredential()
    #     STORAGE_ACCOUNT_NAME = os.getenv("STORAGE_ACCOUNT_NAME")
    #     blob_service_client_url = f"https://{STORAGE_ACCOUNT_NAME}.blob.core.windows.net"
        
    #     blob_service_client = BlobServiceClient(account_url=blob_service_client_url, credential=credential, logging=azure_storage_logger)

    blob_client = blob_service_client.get_blob_client(container=BLOB_CONTAINER_NAME, blob=filename)

    return blob_client
