from faker import Faker
import csv
import datetime
import random
import sample_data

def generate_bss_data():
    fake = Faker()

    num_rows = 1_000_000
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

if __name__ == "__main__":
    generate_bss_data()