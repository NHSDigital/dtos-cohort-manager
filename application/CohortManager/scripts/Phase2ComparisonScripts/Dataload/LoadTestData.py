import psycopg2
import pandas as pd
from sqlalchemy import create_engine
import io

# df = pd.read_csv('testdata.csv')
# df['dateOfDeath'] = df['date_of_death'].replace('NULL', pd.NA)
# df['dateOfDeath'] = pd.to_datetime(df['date_of_death'], errors='coerce', format='%Y-%m-%d')
# # print(df.to_string())

# # connection = psycopg2.connect(database="bsselecttesting", user="postgres", password="mysecretpassword", host="localhost", port=5432)
# for col in df.select_dtypes(include=["object"]):
#     df[col] = df[col].astype(str).str.replace('\x00', '', regex=True)

engine = create_engine('')




csv_file = "testdata.csv"  # Path to your CSV file
df = pd.read_csv(csv_file)

df = df.replace("NULL", pd.NA)
df.columns = df.columns.str.strip()
df['message_id'] = df['message_id'].str.strip()
df['replaced_nhs_number'] = df['replaced_nhs_number'].astype(str).str[:10]
df['superseded_by_nhs_number'] = df['superseded_by_nhs_number'].astype(str).str[:10]

print(df.info())


table_name = "ch_changes"
try:
    df.to_sql(table_name, engine, if_exists="append", index=False)
    print("Data successfully imported into PostgreSQL!")
except Exception as e:
    print(f"Error occurred: {e}")

