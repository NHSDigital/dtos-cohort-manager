"""Script that automatically sets up azurite with the required blob containers and files.
    Used in the azurite-setup container but can also be ran outside of the container."""

import os
from azure.storage.blob import BlobServiceClient, BlobClient
from azure.core.exceptions import ResourceExistsError

def setup_azurite():
    connect_str = os.getenv("AZURITE_CONNECTION_STRING")
    blob_service_client = BlobServiceClient.from_connection_string(connect_str)
    print("Connected to Azurite")

    try:
        blob_service_client.create_container("inbound")
        blob_service_client.create_container("rules")
        blob_service_client.create_container("file-exceptions")
        print("Blob containers created")
    except ResourceExistsError:
        print("Blob containers already exist")

setup_azurite()
