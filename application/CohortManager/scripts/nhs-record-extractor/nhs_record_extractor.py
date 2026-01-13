#!/usr/bin/env python3
"""
NHS Record Extractor

This script finds the most recent records for a list of NHS numbers
from parquet files in the current directory and saves them to a new parquet file.
The most recent record is determined by the date in the filename.
"""

from pandas.core.frame import DataFrame


from typing import Any


import os
import sys
import glob
import pandas as pd
import pyarrow
import pyarrow.parquet as pq
from datetime import datetime
import re

def extract_date_from_filename(filename):
    """
    Extract the date from a filename.
    
    The date is everything up to the first underscore in the filename.
    Example: '20251027100135103118_BEFB67_-_CAAS_BREAST_SCREENING_COHORT.parquet'
    
    Args:
        filename (str): The filename to extract the date from
        
    Returns:
        str: The date string extracted from the filename
    """
    # Get just the filename without the path
    base_filename = os.path.basename(filename)
    
    # Extract everything up to the first underscore
    match = re.match(r'^([^_]+)_', base_filename)
    
    if match:
        return match.group(1)
    else:
        # If no underscore found, return empty string
        return ""

def find_parquet_files(directory='.'):
    """
    Find all parquet files in the specified directory.
    
    Args:
        directory (str): Directory to search for parquet files
        
    Returns:
        list: List of paths to parquet files
    """
    return glob.glob(os.path.join(directory, '*.parquet'))

def read_nhs_numbers(file_path=None):
    """
    Read NHS numbers from a file or use example numbers.
    
    Args:
        file_path (str, optional): Path to file containing NHS numbers
        
    Returns:
        list: List of NHS numbers
    """
    if file_path:
        try:
            with open(file_path, 'r') as file:
                nhs_numbers = [line.strip() for line in file if line.strip()]
            return nhs_numbers
        except FileNotFoundError:
            print(f"Error: File '{file_path}' not found.")
            sys.exit(1)
    else:
        # Example NHS numbers if no file is provided
        return ['4010232137', '1234567890']

def find_most_recent_records(nhs_numbers, parquet_files) -> tuple[DataFrame, Any | None, dict[Any, Any]]:
    """
    Find the most recent record for each NHS number from the parquet files.
    The most recent record is determined by the date in the filename.
    
    Args:
        nhs_numbers (list): List of NHS numbers to search for
        parquet_files (list): List of parquet file paths
        
    Returns:
        tuple: (DataFrame of most recent records, schema of the parquet files)
    """
    # Convert NHS numbers to a set for faster lookup
    nhs_set = set(nhs_numbers)
    
    # Dictionary to store the most recent record for each NHS number
    most_recent_records = {}
    
    # Store the schema from the first valid parquet file
    schema = None
    
    # Process each parquet file
    for file_path in parquet_files:
        print(f"Processing file: {file_path}")
        
        # Extract date from filename
        file_date_str = extract_date_from_filename(file_path)
        
        if not file_date_str:
            print(f"Warning: Could not extract date from filename {file_path}. Skipping.")
            continue
            
        try:
            # Read the parquet file
            df = pd.read_parquet(file_path)
            
            # Store the schema from the first valid parquet file
            if schema is None:
                # Get the schema from the parquet file
                parquet_file = pq.ParquetFile(file_path)
                # Convert ParquetSchema to pyarrow.lib.Schema
                schema = parquet_file.schema_arrow
            
            # Check if the dataframe has the necessary column
            if 'nhs_number' not in df.columns:
                print(f"Warning: File {file_path} does not have an 'nhs_number' column. Skipping.")
                continue
            
            # Filter for records with matching NHS numbers
            matching_records = df[df['nhs_number'].astype(str).isin(list(nhs_set))]
            
            # Process each matching record
            for _, record in matching_records.iterrows():
                nhs = str(record['nhs_number'])
                
                # Check if this is the most recent record for this NHS number
                # based on the file date
                if nhs not in most_recent_records or file_date_str > most_recent_records[nhs]['file_date']:
                    most_recent_records[nhs] = {
                        'record': record,
                        'file_date': file_date_str,
                        'file_path': file_path
                    }
        
        except Exception as e:
            print(f"Error processing file {file_path}: {e}")
    
    # Create a DataFrame from the most recent records
    if most_recent_records:
        records_list = [record_data['record'] for record_data in most_recent_records.values()]
        result_df = pd.DataFrame(records_list)
        return result_df, schema, most_recent_records
    else:
        return pd.DataFrame(), schema, {}

def save_to_parquet(df, schema, output_file='most_recent_records.parquet'):
    """
    Save the DataFrame to a parquet file with the same schema as the source files.
    
    Args:
        df (DataFrame): DataFrame to save
        schema: Schema to use for the parquet file
        output_file (str): Path to save the parquet file
    """
    if df.empty:
        print("No records to save.")
        return False
    
    try:
        # Save the DataFrame to a parquet file
        if schema is not None:
            try:
                # Try to use the provided schema
                table = pyarrow.Table.from_pandas(df, schema=schema)
            except Exception as schema_error:
                print(f"Warning: Could not use provided schema: {schema_error}")
                print("Falling back to inferred schema.")
                table = pyarrow.Table.from_pandas(df)
        else:
            # No schema provided, let pyarrow infer it
            table = pyarrow.Table.from_pandas(df)
            
        pq.write_table(table, output_file)
        print(f"Saved {len(df)} records to {output_file}")
        return True
    except Exception as e:
        print(f"Error saving to parquet file: {e}")
        return False

def main():
    """
    Main function to find the most recent records for NHS numbers and save them to a parquet file.
    """
    # Check if a file path was provided as a command-line argument
    if len(sys.argv) > 1:
        file_path = sys.argv[1]
        print(f"Reading NHS numbers from {file_path}...")
        nhs_numbers = read_nhs_numbers(file_path)
    else:
        nhs_numbers = read_nhs_numbers()
        print(f"Using example NHS numbers: {nhs_numbers}")
    
    # Find all parquet files in the current directory
    parquet_files = find_parquet_files()
    print(f"Found {len(parquet_files)} parquet files.")
    
    if not parquet_files:
        print("No parquet files found in the current directory.")
        sys.exit(1)
    
    # Find the most recent records
    result_df, schema, most_recent_records = find_most_recent_records(nhs_numbers, parquet_files)
    
    # Print summary of results
    if not result_df.empty:
        print(f"\nFound most recent records for {len(result_df)} NHS numbers.")
        
        # Get the output file name
        output_file = 'most_recent_records.parquet'
        if len(sys.argv) > 2:
            output_file = sys.argv[2]
        
        # Print source files for each NHS number
        print("\nSource files for each NHS number:")
        for nhs, data in most_recent_records.items():
            print(f"NHS {nhs}: {data['file_path']} (Date: {data['file_date']})")
        
        # Save the results to a parquet file
        save_to_parquet(result_df, schema, output_file)
    else:
        print("No matching records found for the provided NHS numbers.")

if __name__ == "__main__":
    main()

# Made with Bob
