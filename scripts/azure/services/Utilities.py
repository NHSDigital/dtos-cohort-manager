from typing import Dict

def new_docker_image_slack_payload(function_name: str, pipeline_url: str, image_tags: str) -> Dict:
    return {
        'text': '',
        'blocks': [
            {
                "type": "section",
                "text": {
                    "type": "mrkdwn",
                    "text": f'*FOR YOUR INFORMATION* \n New version of function *{function_name}* with tags *{image_tags}* has been uploaded, <{pipeline_url}| Click here to deploy the latest version of the image with the development tag>'
                }
            }
        ],
    }

def new_function_deployment_slack_payload(function_name: str, image_tags: str) -> Dict:
    return {
        'text': '',
        'blocks': [
            {
                "type": "section",
                "text": {
                    "type": "mrkdwn",
                    "text": f'*FOR YOUR INFORMATION* \n New version of function *{function_name}* has been deployed to the function App with tags *{image_tags}*'
                }
            }
        ],
    }

def new_docker_image_available(function_name, pipeline_url, image_tags, slack):

    payload = new_docker_image_slack_payload(function_name, pipeline_url, image_tags)
    slack.send(payload)

def new_function_deployed(function_name, image_tags, slack):

    payload = new_function_deployment_slack_payload(function_name, image_tags)
    slack.send(payload)

def unstructured_slack_message(file_path, slack):
    slack.send_file_content(file_path, file_path)

