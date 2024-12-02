import os
from azure.storage.blob import BlobServiceClient
import logging
from config import azure_storage_logger

BLOB_CONTAINER_NAME = os.getenv("BLOB_CONTAINER_NAME")

def get_blob_client(filename):
    """
    Sets up the blob client

    Parameters:
        filename (string): the name of the blob

    Returns:
        BlobClient
    """

    CONNECTION_STRING = os.getenv("STORAGE_CONNECTION_STRING")

    blob_service_client = BlobServiceClient.from_connection_string(CONNECTION_STRING, logging=azure_storage_logger)
    blob_client = blob_service_client.get_blob_client(container=BLOB_CONTAINER_NAME, blob=filename)

    return blob_client
