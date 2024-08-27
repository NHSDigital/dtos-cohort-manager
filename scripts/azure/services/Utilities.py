from typing import Dict

def new_docker_image_slack_payload(functionName: str, pipelineURL: str, imageTags: str) -> Dict:
    return {
        'text': '',
        'blocks': [
            {
                "type": "section",
                "text": {
                    "type": "mrkdwn",
                    "text": f'*FOR YOUR INFORMATION* \n New version of function *{functionName}* with tags *{imageTags}* has been uploaded, <{pipelineURL}| Click here to deploy the latest version of the image with the development tag>'
                }
            }
        ],
    }

def new_function_deployment_slack_payload(functionName: str, imageTags: str) -> Dict:
    return {
        'text': '',
        'blocks': [
            {
                "type": "section",
                "text": {
                    "type": "mrkdwn",
                    "text": f'*FOR YOUR INFORMATION* \n New version of function *{functionName}* has been deployed to the function App with tags *{imageTags}*'
                }
            }
        ],
    }

def new_docker_image_available(functionName, pipelineURL, imageTags, slack):

    payload = new_docker_image_slack_payload(functionName, pipelineURL, imageTags)
    slack.send(payload)

def new_function_deployed(functionName, imageTags, slack):

    payload = new_function_deployment_slack_payload(functionName, imageTags)
    slack.send(payload)
