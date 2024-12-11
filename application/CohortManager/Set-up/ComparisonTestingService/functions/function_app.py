import azure.functions as func
import azure.durable_functions as d_func
from config import logger
from insert_data import insert_data_bp
from compare_datasets import compare_datasets_bp

app = d_func.DFApp()
app.register_functions(insert_data_bp)
app.register_functions(compare_datasets_bp)

# TODO: pass in all null values to discrepancy test and see what happens

@app.route(route="orchestrators/{functionName}")
@app.durable_client_input(client_name="client")
async def start(req: func.HttpRequest, client):
    """
    Start function, routes requests to the relevant orchestrator

    Parameters:
        functionName (str): The name of the orchestrator to call
        filename (str): (Optional) The name of the file in blob storage to
                        retrieve. Only required for calls to the insert functions
    """

    filename = req.params.get("filename")
    function_name = req.route_params.get('functionName')
    payload = {"filename": filename}

    instance_id = await client.start_new(function_name, None, payload)
    status_url = client.create_check_status_response(req, instance_id).headers['Location']

    return func.HttpResponse(
        body=f"Redirecting to status page... If not redirected, click <a href='{status_url}'>here</a>.",
        status_code=302,
        headers={'Location': status_url}
    )

@app.orchestration_trigger(context_name="context")
def insert_caas_orchestrator(context: d_func.DurableOrchestrationContext):
    """insert_caas_data orchestrator"""

    input_context = context.get_input()
    filename = input_context.get('filename')
    params = {"filename": filename, "table_name": "CAAS_PARTICIPANT"}

    result = yield context.call_activity("insert_data", params)

    return result

@app.orchestration_trigger(context_name="context")
def insert_bss_orchestrator(context: d_func.DurableOrchestrationContext):
    """insert_bss_data orchestrator"""
    input_context = context.get_input()
    filename = input_context.get('filename')
    params = {"filename": filename, "table_name": "BSS_PARTICIPANT"}

    result = yield context.call_activity("insert_data", params)

    return result

@app.orchestration_trigger(context_name="context")
def compare_datasets_orchestrator(context: d_func.DurableOrchestrationContext):
    """compare_datasets orchestrator"""
    result = yield context.call_activity("compare_datasets")
    return result