import argparse
import sys
from send_sample_file import send_sample_file
import pandas as pd
import numpy as np
from fastparquet import write

parser = argparse.ArgumentParser(description='An script that allows you to edit parquet files',
                                formatter_class=argparse.RawTextHelpFormatter,
                                epilog="""Examples:
    python parquet-editor.py -f BSS_20241201121212_n1.parquet -c nhs_number given_name -v 9442788159 bob -s
    python parquet-editor.py -f BSS_20241201121212_n1.parquet -c family_name -r 0 -v booble""")

parser.add_argument('-f', nargs=1, help='The filename of the parquet file you would like to edit', required=True)
parser.add_argument('-o', nargs=1, help='(OPTIONAL) The output filename', required=False)
parser.add_argument('-c', nargs='+', help='The column(s) to be edited, in snake_case format, separated by spaces', required=True)
parser.add_argument('-r', nargs=1, type=int, help='(Optional) The index of the row to be edited (will overwrite the entire row if left blank)', required=False)
parser.add_argument('-v', nargs='+', help='The value(s) of to set the corresponding column(s) to, separated by spaces', required=True)
parser.add_argument('-s', action='store_true', help='(OPTIONAL) Send the file to azurite')

if len(sys.argv) == 1:
    parser.print_help()
    sys.exit(1)

args = parser.parse_args()

file_name = args.f[0]

schema = {
    "record_type": "string",
    "change_time_stamp": "Int64",
    "serial_change_number": "Int64",
    "nhs_number": "Int64",
    "superseded_by_nhs_number": "Int64",
    "primary_care_provider": "string",
    "primary_care_effective_from_date": "string",
    "current_posting": "string",
    "current_posting_effective_from_date": "string",
    "name_prefix": "string",
    "given_name": "string",
    "other_given_name": "string",
    "family_name": "string",
    "previous_family_name": "string",
    "date_of_birth": "string",
    "gender": "Int64",
    "address_line_1": "string",
    "address_line_2": "string",
    "address_line_3": "string",
    "address_line_4": "string",
    "address_line_5": "string",
    "postcode": "string",
    "paf_key": "string",
    "address_effective_from_date": "string",
    "reason_for_removal": "string",
    "reason_for_removal_effective_from_date": "string",
    "date_of_death": "string",
    "death_status": "Int32",
    "home_telephone_number": "string",
    "home_telephone_effective_from_date": "string",
    "mobile_telephone_number": "string",
    "mobile_telephone_effective_from_date": "string",
    "email_address": "string",
    "email_address_effective_from_date": "string",
    "preferred_language": "string",
    "is_interpreter_required": "boolean",
    "invalid_flag": "boolean",
    "eligibility": "boolean"
}

df = pd.read_parquet(file_name, engine='fastparquet').astype(schema, errors="ignore")

for i in range(len(args.c)):
    column_name = args.c[i]
    value = args.v[i]
    column_type = df[column_name].dtype


    if column_name not in df.columns:
        sys.exit(f"Column {column_name} not in schema, exiting")

    if value == "null":
        value = ""
    else:
        match schema[column_name]:
            case "Int64":
                value = int(value)
            case "boolean":
                value = value.lower() == "true"

    if args.r:
        df.at[args.r[0], column_name] = value
    else:
        df[column_name] = value


    print(f"Column {column_name} value changed to {value}")

if args.o:
    file_name = args.o[0]

df.to_parquet(path=file_name, engine='fastparquet', index=False)

if args.s:
    send_sample_file(file_name)
