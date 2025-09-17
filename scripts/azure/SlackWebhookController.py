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

    container_url, function_deployed_name, tags, _ ='', '', '', ''
    verbose=False
    try:
        opts, _ = getopt.getopt(argv,"hc:f:r:t:v", ["container_url=", "function_deployed_name=", "report=", "tags=","verbose"])
    except getopt.GetoptError:
        print('SlackWebhookController.py -c <container_url> -d <data>  -f <function_deployed_name> -t <tags>')
        sys.exit(2)

    for opt, arg in opts:
        if opt == '-h':
            print('SlackWebhookController.py -c <container_url> -f <function_deployed_name> -r <reportFile> -t <tags> ')
            sys.exit()
        elif opt in ("-c", "--container_url"):
            container_url = arg
        elif opt in ("-r", "--report"):
            report = arg
        elif opt in ("-f", "--functionDeployed"):
            function_deployed_name = arg
        elif opt in ("-t", "--tags"):
            tags = arg
        elif opt in ("-v", "--verbose"):
            verbose=True

    if not function_deployed_name:
        print("Please provide the function name")
        exit(1)

    webhook_url = os.environ.get('SLACK_WEBHOOK_URL')
    slack = SlackWebhookBot(webhook_url)

    if verbose:
        print('function_deployed_name is: ', function_deployed_name)
        print('container_url is: ', container_url)
        print("tags: ", tags)


    if container_url:
        new_docker_image_available(function_deployed_name, container_url, tags, slack)
    elif report:
        unstructured_slack_message(function_deployed_name, report, slack)
    else:
        new_function_deployed(function_deployed_name, tags, slack)


if __name__ == "__main__":
    main(sys.argv[1:])
