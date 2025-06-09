"""A simple scipt to send sample files to azurite
    Requirements:
        python-dotenv
        azure-storage-blob"""

import requests
import sys

def block_participant(nhs_number: str, family_name: str, date_of_birth: str):
    block_url = "http://127.0.0.1:7072/api/BlockParticipant"

    data = {
        "NhsNumber": nhs_number,
        "FamilyName": family_name,
        "DateOfBirth": date_of_birth
    }

    user_respone = input(f"Blocking participant with details {data}, continue (Y/n)? ").strip().lower()
    if user_respone != "y":
        print("Operation cancelled by user.")
        sys.exit(0)

    response = requests.post(block_url, json=data)
    if response.status_code == 200:
        print("Participant blocked successfully.")
    else:
        print(f"Failed to block participant. Status code: {response.status_code}")
        print(f"Response: {response.text}")
        sys.exit(1)
    
def get_config


if __name__ == "__main__":
    if len(sys.argv) < 4:
        print("""Description:
        A script to block a participant
    Usage:
        python block-participant.py NhsNumber FamilyName DateOfBirth
    Arguments:
        NhsNumber  The NHS number of the participant to be blocked
        FamilyName  The family name of the participant to be blocked
        DateOfBirth  The date of birth of the participant to be blocked in YYYYMMDD format
    Examples:
        python block-participant.py 1234567890 Doe 19900101""")
        sys.exit()
    elif len(sys.argv) > 4:
        sys.exit("This script accepts only one argument, run the command with no arguments to view the help page")
    elif sys.argv[1] == "1":
        sample_file = "BSS_20241201121212_n1.parquet"
    elif sys.argv[1] == "10":
        sample_file = "BSS_20240601121212_n10.csv"
    elif sys.argv[1] == "100":
        sample_file = "BSS_20240628191800_n100.csv"
    else:
        sample_file = sys.argv[1]
