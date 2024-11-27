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
except Exception as e:
        logging.error(e)

app = func.FunctionApp()

@app.route(route="InsertCaasData")
def InsertCaasData(req: func.HttpRequest) -> func.HttpResponse:
    """Inserts CaaS data from azure storage into the CAAS_PARTICIPANT table. Triggered manually in azure"""
    logging.info('Insert CaaS Data Function triggered')
    
    table_name = "CAAS_PARTICIPANT"

    return func.HttpResponse("CaaS data inserted", status_code=200)


@app.route(route="InsertBsSelectData")
def InsertBsSelectData(req: func.HttpRequest) -> func.HttpResponse:
    """Inserts BS Select data from azure storage into the BSS_PARTICIPANT table. Triggered manually in azure"""
    print('Insert BS Select Data Function triggered')

    table_name = "BSS_PARTICIPANT"

    return main(table_name, req) or func.HttpResponse("BS Select data inserted", status_code=200)

def main(table_name, req) -> func.HttpResponse:

    # Get filename from request params
    filename = req.params.get("filename")
    
    if filename is None:
        logging.error("Filename not found in request")
        return func.HttpResponse("Please add a filename in the request URL", status_code=400)

    # Set up connections
    try:
        blob_client = storage.get_blob_client(filename)
        db_engine = db.setup_engine()
    except Exception as e:
        logging.error(e)
        return func.HttpResponse("There was an issue establishing the database and/ or storage connections", status_code=500)

    # Read and save file
    try: 
        stream = storage.read_file(blob_client, filename)
    except ResourceNotFoundError:
        logging.error("Blob does not exist")
        return func.HttpResponse("Blob does not exist", status_code=404)
    except Exception as e:
        logging.error(e)
        return func.HttpResponse("Blob does not exist", status_code=500)

    
    # Convert file to DataFrame
    if filename.endswith(".csv"):
        df = pd.read_csv(BytesIO(stream))
        df.rename(columns=BSS_COLUMN_MAPPINGS, inplace=True)
    else:
        df = parquet.to_dataframe(filename)
        df.rename(columns=CAAS_COLUMN_MAPPINGS, inplace=True)

    # Send data to the database
    try:
        df.to_sql(table_name, con=db_engine, if_exists='append', index=False, chunksize=1000)
        logging.info("Data inserted into the database")
    except Exception as e:
        logging.error(e)
        return func.HttpResponse("There was an issue sending the data to the database", status_code=500)
