"""A simple scipt to send sample files to azurite
    Requirements:
        python-dotenv
        azure-storage-blob"""

import requests
import sys
import json
from datetime import datetime
from pprint import pprint
from sqlalchemy import create_engine, text

def block_participant(nhs_number: str, family_name: str, date_of_birth: str):
    block_url = "http://localhost:7027/api/BlockParticipant"

    parsed_date = datetime.strptime(date_of_birth, "%Y-%m-%d")
    block_user_response = input(f"Blocking participant with NHS Number: {nhs_number}, last name: {family_name}, date of birth: {date_of_birth}.\nContinue (Y/N)? ").strip().lower()

    query_params = {
        "NhsNumber": nhs_number,
        "FamilyName": family_name,
        "DateOfBirth": parsed_date.strftime("%Y%m%d")
    }
    if block_user_response != "y":
        print("Operation cancelled by user.")
        sys.exit(0)

    response = requests.post(block_url, params=query_params)
    if response.status_code == 200:
        print("Participant blocked successfully.")
    else:
        print(f"Failed to block participant. Status code: {response.status_code}")
        print(f"Response: {response.text}")
        sys.exit(1)

    delete_records(nhs_number, family_name, parsed_date.strftime("%Y-%m-%d"))

def delete_records(nhs_number: str, family_name: str, date_of_birth: str):
    engine = create_engine("mssql+pyodbc://SA:Password!@127.0.0.1:1433/DToSDB?driver=ODBC+Driver+18+for+SQL+Server&TrustServerCertificate=yes")

    with engine.begin() as connection:
        base_query = f"FROM BS_COHORT_DISTRIBUTION WHERE NHS_NUMBER = '{nhs_number}' AND FAMILY_NAME = '{family_name}' AND DATE_OF_BIRTH = '{date_of_birth}'"
        rows = connection.execute(text(f"SELECT * {base_query}"))
        for row in rows.mappings():
            pprint(dict(row))

        user_response = input("Delete participant record(s)? (Y/N): ")
        if user_response != "y":
            print("Operation cancelled by user.")
            sys.exit(0)
        
        rows_affected = connection.execute(text(f"DELETE {base_query}"))
        print(f"Deleted {rows_affected.rowcount} record(s)")


if __name__ == "__main__":
    if len(sys.argv) < 4:
        print("""Description:
        A script to block a participant
    Usage:
        python block-participant.py NhsNumber FamilyName DateOfBirth
    Arguments:
        NhsNumber  The NHS number of the participant to be blocked
        FamilyName  The family name of the participant to be blocked
        DateOfBirth  The date of birth of the participant to be blocked in YYYY-MM-DD format
    Examples:
        python block-participant.py 1234567890 Doe 19900101""")
        sys.exit()
    elif len(sys.argv) > 4:
        sys.exit("This script accepts only three arguments, run the command with no arguments to view the help page")
    else:
        block_participant(sys.argv[1], sys.argv[2], sys.argv[3])
