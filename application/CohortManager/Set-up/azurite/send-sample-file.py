import sys
import os
from azure.storage.blob import BlobServiceClient, BlobClient

if len(sys.argv) < 2:
    print("""Description:
    A script to send sample files to azurite
Usage:
    python send-sample-file.py file
Arguments:
    file  The file to be sent
Options:
    1  The BSS_20240628191800_n1.csv sample file
    10  The BSS_20240601121212_n10.csv sample file
    100  The BSS_20240628191800_n100.csv sample file
Example:
    python send-sample-file.py 100""")
    sys.exit()
elif len(sys.argv) > 2:
    sys.exit("This script accepts only one argument, run the command with no arguments to view the help page")
elif sys.argv[1] == "1":
    sample_file = "BSS_20240628191800_n1.csv"
elif sys.argv == "10":
    sample_file = "BSS_20240601121212_n10.csv"
elif sys.argv == "100":
    sample_file = "BSS_20240628191800_n100.csv"


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
