import logging
import requests
import os

logger = logging.getLogger()
logger.setLevel(logging.INFO)

class SlackWebhookBot:
    def __init__(self, webhook_url: str, timeout: int = 15):
        self.webhook_url = webhook_url
        self.timeout = timeout
        self.headers = {
            'Content-Type': 'application/json',
        }

    def send_file_content(self, file_path: str, message: str = '') -> bool:
        if not os.path.isfile(file_path):
            logger.error(f'The file at path {file_path} does not exist.')
            return False

        try:
            with open(file_path, 'r') as file:
                file_content = file.read()

            # Create the payload with the file content
            payload = {
                "text": f"{message}\n```{file_content}```"
            }

            return self.send(payload)
        except Exception as e:
            logger.error(f'Error reading or sending file content: {e}')
            return False

    def send(self, payload) -> bool:
        success = False
        try:
            r = requests.post(
                self.webhook_url,
                headers=self.headers,
                json=payload,
                timeout=self.timeout
            )
            if r.status_code == 200:
                success = True
                logger.info('Successfully sent message to Slack.')
            else:
                logger.error(f'Failed to send message to Slack. Status code: {r.status_code}, response: {r.text}')

        except requests.Timeout:
            logger.error('Timeout occurred when trying to send message to Slack.')
        except requests.RequestException as e:
            logger.error(f'Error occurred when communicating with Slack: {e}.')
        else:
            success = True
            logger.info('Successfully sent message to Slack.')

        return success
