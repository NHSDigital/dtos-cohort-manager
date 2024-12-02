import azure.functions as func
import logging

try:
    from io import BytesIO
    from azure.core.exceptions import ResourceNotFoundError
    from config import BSS_COLUMN_MAPPINGS, CAAS_COLUMN_MAPPINGS
    import parquet
    import storage
    import pandas as pd
    import database as db
    import time
except Exception as e:
    logging.error(e)

async def insert_data(table_name, req) -> func.HttpResponse:

    # Get filename from request or assign directly (if called with string filename)
    if isinstance(req, str):
        filename = req
    else:
        filename = req.params.get("filename")
    
    if filename is None:
        logging.error("Filename not found in request")
        return func.HttpResponse("Please add a filename in the request URL", status_code=400)

    # Set up connections
    try:
        logging.info("Establishing connections")
        blob_client = storage.get_blob_client(filename)
        db_engine = db.setup_engine()
    except Exception as e:
        logging.error(e)
        return func.HttpResponse("There was an issue establishing the database and/ or storage connections", status_code=500)

    # Read file
    try:
        logging.info("Reading file")
        stream = blob_client.download_blob().readall()
    except ResourceNotFoundError:
        logging.error("Blob does not exist")
        return func.HttpResponse("Blob does not exist", status_code=404)
    except Exception as e:
        logging.error(e)
        return func.HttpResponse(e, status_code=500)

    
    # Convert file to DataFrame
    if filename.endswith(".csv"):
        df = pd.read_csv(BytesIO(stream))
        df.rename(columns=BSS_COLUMN_MAPPINGS, inplace=True)
    else:
        df = parquet.to_dataframe(BytesIO(stream))
        df.rename(columns=CAAS_COLUMN_MAPPINGS, inplace=True)

    # TODO: Add a warning here
    num_rows_with_dups = df.shape[0]
    df = df.drop_duplicates(subset=['nhs_number', 'date_of_birth'])
    num_rows = df.shape[0]
    logging.warning(f"Dropped {num_rows_with_dups - num_rows} duplicate rows from dataset")

    # Create tables & send data to the database
    try:
        db.create_tables(db_engine)
        logging.info("Inserting data into the database")
        start = time.time()
        await df.to_sql(table_name, con=db_engine, if_exists='append', chunksize=1000, index=False)
        end = time.time()
        logging.info(f"Inserted {num_rows:,} rows into database in {((end - start) / 60):.2f} minutes")
    except Exception as e:
        logging.error(e)
        return func.HttpResponse("There was an issue sending the data to the database", status_code=500)