"""Simple script to send the sample file to azurite so you don't have to have storage explorer open
    Requires the azure-storage-blob package"""


import os
from azure.storage.blob import BlobServiceClient, BlobClient

connect_str = "AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;DefaultEndpointsProtocol=http;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;"
blob_service_client = BlobServiceClient.from_connection_string(connect_str)
print("Connected to Azurite")

inbound_client = blob_service_client.get_container_client("inbound")
sample_file = "BSS_20240601121212_n10.csv"
blob_client = inbound_client.get_blob_client(sample_file)
print("blob client established")

with open(sample_file, "rb") as data:
    blob_client.upload_blob(data, overwrite=True)

print("Sample file uploaded")