import pandas as pd
import sys

if len(sys.argv) < 2:
    print("Usage: python view_parquet.py <parquet_file>")
    print("\nAvailable options:")
    print("  python view_parquet.py <file>           # View first 20 rows")
    print("  python view_parquet.py <file> --all     # View all rows")
    print("  python view_parquet.py <file> --nhs     # View NHS numbers only")
    sys.exit(1)

filename = sys.argv[1]
df = pd.read_parquet(filename)

print(f"\n=== Parquet File: {filename} ===")
print(f"Total records: {len(df)}")
print(f"Columns: {', '.join(df.columns.tolist())}\n")

if "--nhs" in sys.argv:
    print("NHS Numbers (first 50):")
    print(df['nhs_number'].head(50).to_string())
    print(f"\nAll NHS numbers start with 999: {df['nhs_number'].astype(str).str.startswith('999').all()}")
elif "--all" in sys.argv:
    print(df.to_string())
else:
    print("First 20 records:")
    pd.set_option('display.max_columns', None)
    pd.set_option('display.width', None)
    print(df.head(20))
