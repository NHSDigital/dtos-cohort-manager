"""Script that automatically sets up azurite with the required blob containers and files.
    Used in the azurite-setup container but can also be ran outside of the container."""

import os
from azure.storage.blob import BlobServiceClient, BlobClient
from azure.storage.queue import QueueServiceClient
from azure.core.exceptions import ResourceExistsError

def setup_azurite():
    connect_str = os.getenv("AZURITE_CONNECTION_STRING")
    blob_service_client = BlobServiceClient.from_connection_string(connect_str)
    queue_service_client = QueueServiceClient.from_connection_string(connect_str)
    print("Connected to Azurite")

    try:
        blob_service_client.create_container("inbound")
        blob_service_client.create_container("file-exceptions")
        blob_service_client.create_container("nems-updates")
        blob_service_client.create_container("nems-config")
        blob_service_client.create_container("reconciliation-config")
        queue_service_client.create_queue("add-participant-queue")
        queue_service_client.create_queue("add-participant-queue-poison")
        print("Queues & blob containers created")
    except ResourceExistsError:
        print("Queues & blob containers already exist")

setup_azurite()
