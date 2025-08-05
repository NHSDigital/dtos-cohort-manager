#!/usr/bin/env python3
import requests
import os
import json
from datetime import datetime
from pprint import pprint
from dotenv import load_dotenv

class Colors:
    RED = '\033[91m'
    GREEN = '\033[92m'
    YELLOW = '\033[93m'
    END = '\033[0m'

# Load environment variables from .env file
load_dotenv()

# Configuration from environment variables
BLOCK_PARTICIPANT_URL = os.getenv("BLOCK_PARTICIPANT_URL")
PREVIEW_PARTICIPANT_URL = os.getenv("PREVIEW_PARTICIPANT_URL")
DELETE_PARTICIPANT_URL = os.getenv("DELETE_PARTICIPANT_URL")

def validate_environment():
    """Validate that required environment variables are set"""
    required_vars = [
        "BLOCK_PARTICIPANT_URL",
        "PREVIEW_PARTICIPANT_URL",
        "DELETE_PARTICIPANT_URL"
    ]

    missing_vars = [var for var in required_vars if not os.getenv(var)]
    if missing_vars:
        print(f"{Colors.RED}Missing required environment variables: {', '.join(missing_vars)}{Colors.END}")
        print(f"{Colors.RED}Please configure these in your Azure environment or .env file{Colors.END}")
        return False
    return True

def validate_dob_format(dob):
    """Validate that DOB is in YYYYMMDD format and is a valid date"""
    try:
        datetime.strptime(dob, "%Y%m%d")
        return True
    except ValueError:
        return False

def format_dob_for_display(dob):
    """Convert YYYYMMDD to YYYY-MM-DD for display purposes"""
    return f"{dob[:4]}-{dob[4:6]}-{dob[6:8]}"

def block_participant(nhs_number, dob, family_name):
    try:
        params = {
            "NhsNumber": nhs_number,
            "DateOfBirth": dob,
            "FamilyName": family_name
        }
        response = requests.post(BLOCK_PARTICIPANT_URL, params=params)
        response.raise_for_status()
        print(f"{Colors.GREEN}Blocked flag updated successfully{Colors.END}")
        return True
    except requests.exceptions.RequestException as e:
        print(f"{Colors.RED}Error calling BlockParticipant: {e}{Colors.END}")
        if e.response:
            print(f"{Colors.RED}Response: {e.response.text}{Colors.END}")
        return False

def preview_participant(nhs_number, dob, family_name):
    try:
        formatted_dob = f"{dob[:4]}-{dob[4:6]}-{dob[6:8]}"

        payload = {
            "NhsNumber": nhs_number,
            "DateOfBirth": formatted_dob,
            "FamilyName": family_name
        }
        response = requests.post(PREVIEW_PARTICIPANT_URL, json=payload)
        response.raise_for_status()

        data = response.json()
        print(f"\n{Colors.GREEN}Preview Results:{Colors.END}")
        for record in data:
            print(json.dumps(record, indent=2))

        confirm = input("\nDo you want to block the participant and delete these records? (Y/N): ").strip().upper()
        return confirm == "Y"
    except requests.exceptions.RequestException as e:
        print(f"{Colors.RED}Error calling PreviewParticipant: {e}{Colors.END}")
        if e.response:
            print(f"{Colors.RED}Response: {e.response.text}{Colors.END}")
        return False

def delete_participant(nhs_number, dob, family_name):
    try:
        formatted_dob = f"{dob[:4]}-{dob[4:6]}-{dob[6:8]}"

        payload = {
            "NhsNumber": nhs_number,
            "DateOfBirth": formatted_dob,
            "FamilyName": family_name
        }
        response = requests.post(DELETE_PARTICIPANT_URL, json=payload)
        response.raise_for_status()
        print(f"{Colors.GREEN}Participants deleted successfully{Colors.END}")
        return True
    except requests.exceptions.RequestException as e:
        print(f"{Colors.RED}Error calling DeleteParticipant: {e}{Colors.END}")
        if e.response:
            print(f"{Colors.RED}Response: {e.response.text}{Colors.END}")
        return False

def main():
    # Validate environment before proceeding
    if not validate_environment():
        return

    print("\nParticipant Management Script\n")

    # Get user input
    nhs_number = input("Enter NHS Number: ")

    while True:
        dob = input("Enter Date of Birth (YYYYMMDD): ")
        if validate_dob_format(dob):
            break
        print(f"{Colors.RED}Invalid date format. Please use YYYYMMDD (e.g., 19600111){Colors.END}")

    family_name = input("Enter Family Name: ")

    print(f"\nUsing Date of Birth: {format_dob_for_display(dob)}")

    # Step 1: Preview participant
    print("\nStep 1: Previewing records...")
    if not preview_participant(nhs_number, dob, family_name):
        print(f"\n{Colors.YELLOW}Blocking and Deletion cancelled.{Colors.END}")
        return

    # Step 2: Block participant
    print("\nStep 2: Blocking participant...")
    if not block_participant(nhs_number, dob, family_name):
        print(f"{Colors.RED}Blocking failed. Exiting.{Colors.END}")
        return

    # Step 3: Delete participant
    print("\nStep 3: Deleting records...")
    delete_participant(nhs_number, dob, family_name)

if __name__ == "__main__":
    main()

