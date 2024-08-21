"""A simple scipt to send sample files to azurite
    Requirements:
        python-dotenv
        azure-storage-blob"""

try:
    import sys
    import os
    from azure.storage.blob import BlobServiceClient
    from dotenv  import load_dotenv
except ModuleNotFoundError:
    sys.exit("Requirements not installed, please run 'pip install azure-storage-blob python-dotenv'")

if len(sys.argv) < 2:
    print("""Description:
    A script to send sample files to azurite
Usage:
    python send-sample-file.py file
Arguments:
    file  The file to be sent to azurite
Options:
    1  The BSS_20240628191800_n1.csv sample file
    10  The BSS_20240601121212_n10.csv sample file
    100  The BSS_20240628191800_n100.csv sample file
    <other file> Any argument not in the list above will be treated as a path to a custom file
Examples:
    python send-sample-file.py 100
    python send-sample-file.py custom-file.csv""")
    sys.exit()
elif len(sys.argv) > 2:
    sys.exit("This script accepts only one argument, run the command with no arguments to view the help page")
elif sys.argv[1] == "1":
    sample_file = "BSS_20240628191800_n1.csv"
elif sys.argv[1] == "10":
    sample_file = "BSS_20240601121212_n10.csv"
elif sys.argv[1] == "100":
    sample_file = "BSS_20240628191800_n100.csv"
else:
    sample_file = sys.argv[1]

try:
    load_dotenv(dotenv_path="../../.env")
    CONNECTION_STRING = os.getenv('AZURITE_CONNECTION_STRING')
    blob_service_client = BlobServiceClient.from_connection_string(CONNECTION_STRING)
    print("Connected to Azurite")
except FileNotFoundError:
    sys.exit(".env file not found, please follow the instructions in the docs to create one.")
except ValueError:
    sys.exit("Could not find the azurite connection string in the .env file, please make sure you have the correct one")

inbound_client = blob_service_client.get_container_client("inbound")
blob_client = inbound_client.get_blob_client(sample_file)
print("blob client established")

try:
    with open(sample_file, "rb") as data:
        blob_client.upload_blob(data, overwrite=True)
except FileNotFoundError:
    sys.exit(f"File {sample_file} could not been found, please download the files from confluence and put them in the directory")

print("Sample file uploaded")
