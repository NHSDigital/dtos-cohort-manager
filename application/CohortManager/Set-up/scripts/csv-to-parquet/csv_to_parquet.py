# csv_to_parquet.py

import pandas as pd
import pyarrow as pa
import pyarrow.parquet as pq

csv_file = 'BSS_21241009115300_n1.csv'
parquet_file = 'BSS_21241009115300_n1.parquet'
parquet_schema = "cohort_dtos_no_index.parquet"
chunksize = 100_000

csv_stream = pd.read_csv(csv_file, sep=',', chunksize=chunksize, low_memory=False, dtype ={
'Record Type': 'string',
'Change Time Stamp': 'Int64',
'Serial Change Number': 'int64',
'NHS Number': 'int64',
'Superseded by NHS number': 'Int64',
'Primary Care Provider ': 'string',
'Primary Care Provider Business Effective From Date': 'string',
'Current Posting': 'string',
'Current Posting Business Effective From Date': 'string',
'Name Prefix': 'string',
'Given Name ': 'string',
'Other Given Name(s) ': 'string',
'Family Name ': 'string',
'Previous Family Name ': 'string',
'Date of Birth': 'string',
'Gender': 'int64',
'Address line 1': 'string',
'Address line 2': 'string',
'Address line 3': 'string',
'Address line 4': 'string',
'Address line 5': 'string',
'Postcode': 'string',
'PAF key': 'string',
'Usual Address Business Effective From Date': 'string',
'Reason for Removal': 'string',
'Reason for Removal Business Effective From Date': 'string',
'Date of Death': 'string',
'Telephone Number (Home)': 'string',
'Telephone Number (Home) Business Effective From Date': 'string',
'Telephone Number (Mobile)': 'string',
'Telephone Number (Mobile) Business Effective From Date': 'string',
'E-mail address (Home)': 'string',
'E-mail address (Home) Business Effective From Date': 'string',
'Preferred Language': 'string',
'Interpreter required': 'string',
'Invalid Flag': 'bool',
})


for i, chunk in enumerate(csv_stream):
    print("Chunk", i)
    if i == 0:
        schema = pa.parquet.read_schema(parquet_schema, memory_map=True)
        schema = schema.set(4,pa.field('superseded_by_nhs_number',pa.int32()))
        schema = schema.set(27,pa.field('death_status',pa.int32()))

        pdschema = pd.DataFrame(({"column": name, "pa_dtype": str(pa_dtype)} for name, pa_dtype in zip(schema.names, schema.types)))
        pdschema = pdschema.reindex(columns=["column", "pa_dtype"], fill_value=pd.NA)
        print(pdschema)
        parquet_writer = pq.ParquetWriter(parquet_file, schema, compression='snappy')

    # Write CSV chunk to the parquet file

    chunk = chunk.rename(columns = {
    'Record Type': 'record_type',
    'Change Time Stamp': 'change_time_stamp',
    'Serial Change Number': 'serial_change_number',
    'NHS Number': 'nhs_number',
    'Superseded by NHS number': 'superseded_by_nhs_number',
    'Primary Care Provider ': 'primary_care_provider',
    'Primary Care Provider Business Effective From Date': 'primary_care_effective_from_date',
    'Current Posting': 'current_posting',
    'Current Posting Business Effective From Date': 'current_posting_effective_from_date',
    'Name Prefix': 'name_prefix',
    'Given Name ': 'given_name',
    'Other Given Name(s) ': 'other_given_name',
    'Family Name ': 'family_name',
    'Previous Family Name ': 'previous_family_name',
    'Date of Birth': 'date_of_birth',
    'Gender': 'gender',
    'Address line 1': 'address_line_1',
    'Address line 2': 'address_line_2',
    'Address line 3': 'address_line_3',
    'Address line 4': 'address_line_4',
    'Address line 5': 'address_line_5',
    'Postcode': 'postcode',
    'PAF key': 'paf_key',
    'Usual Address Business Effective From Date': 'address_effective_from_date',
    'Reason for Removal': 'reason_for_removal',
    'Reason for Removal Business Effective From Date': 'reason_for_removal_effective_from_date',
    'Date of Death': 'date_of_death',
    'Death Status': 'death_status',
    'Telephone Number (Home)': 'home_telephone_number',
    'Telephone Number (Home) Business Effective From Date': 'home_telephone_effective_from_date',
    'Telephone Number (Mobile)': 'mobile_telephone_number',
    'Telephone Number (Mobile) Business Effective From Date': 'mobile_telephone_effective_from_date',
    'E-mail address (Home)': 'email_address',
    'E-mail address (Home) Business Effective From Date': 'email_address_effective_from_date',
    'Preferred Language': 'preferred_language',
    'Interpreter required': 'is_interpreter_required',
    'Invalid Flag': 'invalid_flag'})

    print(chunk.head())
    chunk['primary_care_effective_from_date'] = chunk['primary_care_effective_from_date'].astype('str')
    chunk['current_posting_effective_from_date'] = chunk['current_posting_effective_from_date'].astype('str')
    chunk['date_of_birth'] = chunk['date_of_birth'].astype('str')
    chunk['paf_key'] = chunk['paf_key'].astype('str')
    chunk['is_interpreter_required'] = chunk['is_interpreter_required'].astype('str')

    table = pa.Table.from_pandas(chunk, schema=schema, preserve_index=False)
    parquet_writer.write_table(table)

parquet_writer.close()
