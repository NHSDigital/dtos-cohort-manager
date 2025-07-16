#!/usr/bin/env python3
import requests
import sys
import json
from datetime import datetime
from pprint import pprint

# Configuration
BLOCK_PARTICIPANT_URL = "http://localhost:7027/api/BlockParticipant"
PREVIEW_PARTICIPANT_URL = "http://localhost:7087/api/PreviewParticipant"
DELETE_PARTICIPANT_URL = "http://localhost:7087/api/DeleteParticipant"

def block_participant(nhs_number, dob, family_name):
    try:
        params = {
            "NhsNumber": nhs_number,
            "DateOfBirth": dob,
            "FamilyName": family_name
        }
        response = requests.post(BLOCK_PARTICIPANT_URL, params=params)
        response.raise_for_status()  # Raises HTTPError for bad responses
        print("Blocked flag updated successfully")
        return True
    except requests.exceptions.RequestException as e:
        print(f"Error calling BlockParticipant: {e}")
        if e.response:
            print(f"Response: {e.response.text}")
        return False

def preview_participant(nhs_number, dob, family_name):
    try:
        payload = {
            "NhsNumber": nhs_number,
            "DateOfBirth": dob,
            "FamilyName": family_name
        }
        response = requests.post(PREVIEW_PARTICIPANT_URL, json=payload)
        response.raise_for_status()

        data = response.json()
        print("\n🔍 Preview Results:")
        for record in data:
            print(json.dumps(record, indent=2))

        confirm = input("\nDo you want to delete these records? (Y/N): ").strip().upper()
        return confirm == "Y"
    except requests.exceptions.RequestException as e:
        print(f"Error calling PreviewParticipant: {e}")
        if e.response:
            print(f"Response: {e.response.text}")
        return False

def delete_participant(nhs_number, dob, family_name):
    try:
        payload = {
            "NhsNumber": nhs_number,
            "DateOfBirth": dob,
            "FamilyName": family_name
        }
        response = requests.post(DELETE_PARTICIPANT_URL, json=payload)
        response.raise_for_status()
        print("Participants deleted successfully")
        return True
    except requests.exceptions.RequestException as e:
        print(f"Error calling DeleteParticipant: {e}")
        if e.response:
            print(f"Response: {e.response.text}")
        return False

def main():
    print("\nParticipant Management Script\n")
    nhs_number = input("Enter NHS Number: ")
    dob = input("Enter Date of Birth (YYYY-MM-DD): ")
    family_name = input("Enter Family Name: ")

    # Step 1: Block participant
    print("\nStep 1: Blocking participant...")
    if not block_participant(nhs_number, dob, family_name):
        print("Blocking failed. Exiting.")
        return

    # Step 2: Preview participants before deletion
    print("\nStep 2: Previewing records before deletion...")
    if not preview_participant(nhs_number, dob, family_name):
        print("\nDeletion cancelled.")
        return

    # Step 3: Delete participants
    print("\nStep 3: Deleting records...")
    delete_participant(nhs_number, dob, family_name)

if __name__ == "__main__":
    main()
