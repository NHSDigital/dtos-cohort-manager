import azure.functions as func
import azure.durable_functions as d_func
import logging

app = d_func.DFApp()

try:
    from insert_data import insert_data
    from database import setup_engine, get_table
    from discrepency_flags import flag_discrepencies, preprocess_data, get_unique_rows
    import asyncio
except Exception as e:
    logging.error(e)

# TODO: Figure out behaviour for appending data
# TODO: Add warning for how many rows were duplicates
# TODO: Create user documentation
# TODO: Create test files
# TODO: Load testing

@app.route(route="orchestrators/orchestrator1")
@app.durable_client_input(client_name="client")
async def start(req: func.HttpRequest, client):
    function_name = req.route_params.get('functionName')
    instance_id = await client.start_new(function_name)

    response = client.create_check_status_response(req, instance_id)
    return response

@app.orchestration_trigger(context_name="context")
def orchestrator(context):
    result = yield context.call_activity("InsertBsSelectData", )

    return result


# @app.route(route="InsertCaasData")
# def insert_caas_data(req: func.HttpRequest) -> func.HttpResponse:
#     """Inserts CaaS data from azure storage into the CAAS_PARTICIPANT table. Triggered manually in azure"""
    
#     table_name = "CAAS_PARTICIPANT"

#     return insert_data(table_name, req) or func.HttpResponse("CaaS data inserted", status_code=200)


@app.activity_trigger(route="InsertBsSelectData")
async def insert_bs_select_data(filename: str) -> func.HttpResponse:
    """Inserts BS Select data from azure storage into the BSS_PARTICIPANT table. Triggered manually in azure"""

    table_name = "BSS_PARTICIPANT"

    asyncio.create_task(insert_data(table_name, req))


# @app.route(route="CompareDatasets")
# def compare_datasets(req: func.HttpRequest) -> func.HttpResponse:
#     """Creates a table with the participants that are in the CaaS table but not in the BS Select table. Triggered manually in azure"""
#     # Set-up
#     try:
#         db_engine = setup_engine()
#     except Exception as e:
#         logging.error(e)
#         return func.HttpResponse("There was an issue establishing the database connection", status_code=500)

#     # Reading data from DB
#     try:
#         caas_df = get_table("CAAS_PARTICIPANT", db_engine)
#         bss_df = get_table("BSS_PARTICIPANT", db_engine)
#     except:
#         logging.error(e)
#         return func.HttpResponse("There was an issue reading the data from the database", status_code=500)
    
#     caas_df = preprocess_data(caas_df)
#     bss_df = preprocess_data(bss_df)

#     caas_only_df, bss_only_df = get_unique_rows(caas_df, bss_df)

#     # Add discrepancy categories
#     bss_only_df = flag_discrepencies(bss_only_df)
#     caas_only_df = flag_discrepencies(caas_only_df)
    
#     # Send to DB
#     try:
#         caas_only_df.to_sql("CAAS_ONLY_PARTICIPANTS", con=db_engine, if_exists='append', index=False, chunksize=1000)
#         bss_only_df.to_sql("BSS_ONLY_PARTICIPANTS", con=db_engine, if_exists='append', index=False, chunksize=1000)
#         logging.info("Data inserted into the database")
#     except Exception as e:
#         logging.error(e)
#         return func.HttpResponse("There was an issue saving the data to the database", status_code=500)

#     # Return as formatted HTML tables
#     df_html = f"""
#     <!DOCTYPE html>
#     <html>
#     <head><title>Comparison Data</title></head>
#     <body>
#         <h2>Participants only in CaaS data:</h2>
#         {caas_only_df.to_html(index=False, classes='table table-stripped')}
#         <h2>Participants only in BS Select data:</h2>
#         {bss_only_df.to_html(index=False, classes='table table-stripped')}
#     </body>
#     </html>
#     """
    
#     return func.HttpResponse(df_html, mimetype='text/html', status_code=200)

# @app.route(route="InsertAndCompare")
# def insert_and_compare(req: func.HttpRequest) -> func.HttpResponse:
#     caas_filename = req.params.get("caas-filename")
#     bss_filename = req.params.get("bss-filename")

#     insert_data("CAAS_PARTICIPANT", caas_filename)
#     insert_data("BSS_PARTICIPANT", bss_filename)

#     return compare_datasets()
