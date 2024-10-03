import logging
import sys, getopt
import os
import subprocess
from pathlib import Path
from services.SlackWebhookBot import SlackWebhookBot
from services.Utilities import new_docker_image_available, new_function_deployed, unstructured_slack_message


logger = logging.getLogger()
logger.setLevel(logging.INFO)

##################################

def main(argv):

    containerURL, functionDeployedName, tags, data ='', '', '', ''
    verbose=False
    try:
        opts, args = getopt.getopt(argv,"hc:d:f:t:v",["containerURL=", "data=","functionDeployedName=", "tags=","verbose"])
    except getopt.GetoptError:
        print('SlackWebhookController.py -c <containerURL> -d <data>  -f <functionDeployedName> -t <tags>')
        sys.exit(2)

    for opt, arg in opts:
        if opt == '-h':
            print('SlackWebhookController.py -c <containerURL> -d <data> -f <functionDeployedName> -t <tags> ')
            sys.exit()
        elif opt in ("-c", "--containerURL"):
            containerURL = arg
        elif opt in ("-d", "--data"):
            data = arg
        elif opt in ("-f", "--functionDeployed"):
            functionDeployedName = arg
        elif opt in ("-t", "--tags"):
            tags = arg
        elif opt in ("-v", "--verbose"):
            verbose=True

    if not functionDeployedName:
        print("Please provide the function name")
        exit(1)

    webhook_url = os.environ.get('SLACK_WEBHOOK_URL')
    slack = SlackWebhookBot(webhook_url)

    # Read the contents of the file
    file_path = data
    with open(file_path, 'r') as file:
        file_contents = file.read()

    # Define the command and pass the file contents as an argument
    command = ['echo', file_contents]  # Replace 'echo' with your actual command

    # Execute the command
    result = subprocess.run(command, capture_output=True, text=True)

    if verbose:
        print('functionDeployedName is: ', functionDeployedName)
        print('containerURL is: ', containerURL)
        print("tags: ", tags)
        print("data: ", result.stdout)


    # if containerURL:
    #     new_docker_image_available(functionDeployedName, containerURL, tags, slack)
    # else:
    #     new_function_deployed(functionDeployedName, tags, slack)

    unstructured_slack_message(functionDeployedName, file_path, slack)

if __name__ == "__main__":
    main(sys.argv[1:])
