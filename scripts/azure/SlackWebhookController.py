import logging
import sys, getopt
import os
from pathlib import Path
from services.SlackWebhookBot import SlackWebhookBot
from services.Utilities import new_docker_image_available, new_function_deployed


logger = logging.getLogger()
logger.setLevel(logging.INFO)

##################################

def main(argv):

    containerURL, functionDeployedName, tags='', '', ''
    verbose=False
    try:
        opts, args = getopt.getopt(argv,"hc:f:t:v",["containerURL=", "functionDeployedName=", "tags=","verbose"])
    except getopt.GetoptError:
        print('SlackWebhookController.py -c <containerURL> -f <functionDeployedName> -t <tags>')
        sys.exit(2)

    for opt, arg in opts:
        if opt == '-h':
            print('SlackWebhookController.py -c <containerURL> -f <functionDeployedName> -t <tags> ')
            sys.exit()
        elif opt in ("-c", "--containerURL"):
            containerURL = arg
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

    if verbose:
        print('functionDeployedName is: ', functionDeployedName)
        print('containerURL is: ', containerURL)
        print("tags : ", tags)

    if containerURL:
        new_docker_image_available(functionDeployedName, containerURL, tags, slack)
    else:
        new_function_deployed(functionDeployedName, tags, slack)

if __name__ == "__main__":
    main(sys.argv[1:])
