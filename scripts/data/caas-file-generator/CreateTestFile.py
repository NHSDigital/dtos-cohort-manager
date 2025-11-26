import nhs_number

import argparse
import pandas as pd
import pyarrow as pa
import pyarrow.parquet as pq
from faker import Faker

fake = Faker("en_GB")


# Parse command-line arguments
parser = argparse.ArgumentParser(description="Generate test data for NHS numbers.")
parser.add_argument(
    "--recordCount",
    "-c",
    type=int,
    default=30_000,  # Default value if not provided
    help="Number of records to generate (default: 30,000)",
)
args = parser.parse_args()

# Use the parsed value for recordCount
recordCount = args.recordCount


def generate_unique_nhs_numbers(record_count):
    import random

    excluded_numbers = set()
    try:
        with open("ADD_NHS_NUMBERS.txt", "r") as file:
            excluded_numbers = {line.strip() for line in file if line.strip()}
    except FileNotFoundError:
        print("No File yet!")
    except Exception as e:
        raise RuntimeError(f"Error reading exclusion file: {e}")
    unique_numbers = set()

    while len(unique_numbers) < record_count:
        # Generate 10 digit number starting with 999
        nhs_num = f"999{random.randint(0, 9999999):07d}"

        # Only add if not in excluded numbers
        if nhs_num not in excluded_numbers:
            unique_numbers.add(nhs_num)

    return list(unique_numbers)


def create_data(numbers, recordType):
    new_data = [
        {
            "record_type": recordType,
            "nhs_number": int(nhsNumber),
            "change_time_stamp": None,
            "serial_change_number": index,
            "superseded_by_nhs_number": None,
            "primary_care_provider": "E85121",
            "primary_care_effective_from_date": "20030318",
            "current_posting": "CH",
            "current_posting_effective_from_date": "20130319",
            "name_prefix": "MRS",
            "given_name": fake.first_name(),
            "other_given_name": fake.first_name(),
            "family_name": fake.last_name(),
            "previous_family_name": fake.last_name(),
            "date_of_birth": fake.date_of_birth().strftime("%Y%m%d"),
            "gender": 1,
            "address_line_1": fake.building_number(),
            "address_line_2": fake.street_name(),
            "address_line_3": fake.city(),
            "address_line_4": fake.county(),
            "address_line_5": fake.country(),
            "postcode": "GU10 4SN",
            "paf_key": "Z3S4Q5X9",
            "address_effective_from_date": "",
            "reason_for_removal": None,
            "reason_for_removal_effective_from_date": None,
            "date_of_death": None,
            "death_status": None,
            "home_telephone_number": fake.phone_number(),
            "home_telephone_effective_from_date": "20240501",
            "mobile_telephone_number": fake.phone_number(),
            "mobile_telephone_effective_from_date": "20240501",
            "email_address": fake.email(),
            "email_address_effective_from_date": "20240501",
            "preferred_language": "en",
            "is_interpreter_required": False,
            "invalid_flag": False,
            "eligibility": True,
        }
        for index, nhsNumber in enumerate(numbers)
    ]
    return new_data


def create_parquet_file(data, filename):
    parquet_file = filename
    parquet_schema = "cohort_dtos_no_index.parquet"

    schema = pa.parquet.read_schema(parquet_schema, memory_map=True)
    schema = schema.set(4, pa.field("superseded_by_nhs_number", pa.int64()))
    schema = schema.set(27, pa.field("death_status", pa.int32()))

    pdschema = pd.DataFrame(
        (
            {"column": name, "pa_dtype": str(pa_dtype)}
            for name, pa_dtype in zip(schema.names, schema.types)
        )
    )
    pdschema = pdschema.reindex(columns=["column", "pa_dtype"], fill_value=pd.NA)
    print(pdschema)

    parquet_writer = pq.ParquetWriter(parquet_file, schema, compression="snappy")

    df = pd.DataFrame()
    df = pd.DataFrame(data)
    print(df)
    table = pa.Table.from_pandas(df, schema=schema, preserve_index=False)
    parquet_writer.write_table(table)
    parquet_writer.close()


numbers = generate_unique_nhs_numbers(record_count=recordCount)

add_data = create_data(numbers, "ADD")
# Ammend_data = create_data(numbers,"AMENDED")
# create_parquet_file(Ammend_data,"AMMENDED_LOADFILE_5_-_CAAS_BREAST_SCREENING_COHORT.parquet")
create_parquet_file(
    add_data,
    f"ADD_LOADFILE_P2_{recordCount:06d}_-_CAAS_BREAST_SCREENING_COHORT.parquet",
)

with open("ADD_NHS_NUMBERS.txt", "w") as f:
    for line in numbers:
        f.write("%s\n" % line)
# create_parquet_file(Ammend_data,"AMENDED_60_000-_CAAS_BREAST_SCREENING_COHORT.parquet")
