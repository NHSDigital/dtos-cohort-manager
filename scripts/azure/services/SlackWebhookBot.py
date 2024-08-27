import logging
import requests

logger = logging.getLogger()
logger.setLevel(logging.INFO)

class SlackWebhookBot:
    def __init__(self, webhook_url: str, timeout: int = 15):
        self.webhook_url = webhook_url
        self.timeout = timeout
        self.headers = {
            'Content-Type': 'application/json',
        }

    def send(self, payload) -> bool:
        success = False
        try:
            r = requests.post(
                self.webhook_url,
                headers=self.headers,
                json=payload,
                timeout=self.timeout
            )
        except requests.Timeout:
            logger.error('Timeout occurred when trying to send message to Slack.')
        except requests.RequestException as e:
            logger.error(f'Error occurred when communicating with Slack: {e}.')
        else:
            success = True
            logger.info('Successfully sent message to Slack.')

        return success
