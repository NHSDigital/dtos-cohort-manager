from faker import Faker
import csv
import datetime
import random
import sample_data
import pandas as pd
import numpy as np

def generate_bss_data():
    fake = Faker()

    num_rows = 300_000
    filename = 'bss_load_test_data.csv'

    column_names = ["nhs_number", "date_of_birth", "gp_practice_code", 
                    "removal_reason", "removal_date", "date_of_death",
                    "postcode", "gender_code", "is_higher_risk"]
    

    with open(filename, mode='w', newline='', encoding='utf-8') as file:
        writer = csv.DictWriter(file, fieldnames=column_names)
        
        # Write the header row
        writer.writeheader()
        
        # Generate and write each row
        for _ in range(num_rows):
            writer.writerow({
                'nhs_number': fake.random_int(min=1000000000, max=9999999999),
                'date_of_birth': fake.date_of_birth(minimum_age=40, maximum_age=80),
                'gp_practice_code': random.choice(sample_data.gp_practices),
                'removal_reason': random.choice(sample_data.reasons_for_removal),
                'removal_date': fake.date(),
                'date_of_death': fake.date(),
                'postcode': fake.postcode(),
                'gender_code': random.choice(sample_data.genders),
                'is_higher_risk': fake.boolean()
            })

def generate_caas_data():
    fake = Faker()

    filename = "caas_load_test_10_mill.parquet" 
    num_rows = 10_000_000
    date_format = "%Y-%m-%d"

    data = {
            "record_type": ["ADD" for _ in range(num_rows)],
            "change_time_stamp": [1234 for _ in range(num_rows)],
            "serial_change_number": [5678 for _ in range(num_rows)],
            "nhs_number": [fake.random_int(min=1000000000, max=9999999999) for _ in range(num_rows)],
            "superseded_by_nhs_number": [None for _ in range(num_rows)],
            "primary_care_provider": [random.choice(sample_data.gp_practices) for _ in range(num_rows)],
            "primary_care_effective_from_date": [fake.date() for _ in range(num_rows)],
            "current_posting": [fake.lexify(text='?' * 3)  for _ in range(num_rows)],
            "current_posting_effective_from_date": [fake.date() for _ in range(num_rows)],
            "name_prefix": [fake.prefix() for _ in range(num_rows)],
            "given_name": [fake.first_name() for _ in range(num_rows)],
            "other_given_name": [fake.first_name() for _ in range(num_rows)],
            "family_name": [fake.last_name() for _ in range(num_rows)],
            "previous_family_name": [fake.last_name() for _ in range(num_rows)],
            "date_of_birth": [fake.date_of_birth(minimum_age=40, maximum_age=80).strftime(date_format) for _ in range(num_rows)],
            "gender": [random.choice([1, 2, 9]) for _ in range(num_rows)],
            "address_line_1": [fake.street_address()  for _ in range(num_rows)],
            "address_line_2": [fake.secondary_address() for _ in range(num_rows)],
            "address_line_3": [fake.city() for _ in range(num_rows)],
            "address_line_4": [fake.state() for _ in range(num_rows)],
            "address_line_5": [np.nan for _ in range(num_rows)],
            "postcode": [fake.postcode() for _ in range(num_rows)],
            "paf_key": [np.nan for _ in range(num_rows)],
            "address_effective_from_date": [fake.date() for _ in range(num_rows)],
            "reason_for_removal": [random.choice(sample_data.reasons_for_removal) for _ in range(num_rows)],
            "reason_for_removal_effective_from_date": [fake.date() for _ in range(num_rows)],
            "date_of_death": [np.nan for _ in range(num_rows)],
            "death_status": [0 for _ in range(num_rows)],
            "home_telephone_number": [fake.phone_number() for _ in range(num_rows)],
            "home_telephone_effective_from_date": [fake.date() for _ in range(num_rows)],
            "mobile_telephone_number": [fake.phone_number() for _ in range(num_rows)],
            "mobile_telephone_effective_from_date": [fake.date() for _ in range(num_rows)],
            "email_address": [fake.email() for _ in range(num_rows)],
            "email_address_effective_from_date": [fake.date() for _ in range(num_rows)],
            "preferred_language": [fake.word() for _ in range(num_rows)],
            "is_interpreter_required": [0 for _ in range(num_rows)],
            "invalid_flag": [0 for _ in range(num_rows)],
            "eligibility": [1 for _ in range(num_rows)]
    }

    df = pd.DataFrame(data)

    df.to_parquet(filename, engine='fastparquet', index=False)

if __name__ == "__main__":
    # generate_bss_data()
    generate_caas_data()