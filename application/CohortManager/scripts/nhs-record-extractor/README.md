# NHS Record Extractor

A Python script that finds the most recent records for a list of NHS numbers from parquet files in the current directory and saves them to a new parquet file. The most recent record is determined by the date in the filename.

## Requirements

Install the required dependencies:

```sh
pip install -r requirements.txt
```

## Usage

### Using example NHS numbers

```sh
python nhs_record_extractor.py
```

### Using NHS numbers from a file

```sh
python nhs_record_extractor.py nhs_numbers.txt
```

### Specifying an output file

```sh
python nhs_record_extractor.py nhs_numbers.txt output_file.parquet
```

## Input Format

The input file should contain one NHS number per line.

## How It Works

1. The script scans the current directory for all .parquet files
2. It extracts the date from each filename (everything before the first underscore)
   - Example: From '20251027100135103118_BEFB67_-_CAAS_BREAST_SCREENING_COHORT.parquet', it extracts '20251027100135103118'
3. It reads each parquet file and looks for records matching the provided NHS numbers
4. For each NHS number, it keeps track of the most recent record based on the date extracted from the filename
5. Finally, it saves all the most recent records to a new parquet file with the same schema as the source files

## Expected File Format

### Parquet Files

The script expects parquet files to have at least this column:

- `nhs_number`: The NHS number

### Filenames

The script expects parquet filenames to follow this pattern:

- `DATE_other_information.parquet` where DATE is the date received
- Example: '20251027100135103118_BEFB67_-_CAAS_BREAST_SCREENING_COHORT.parquet'
