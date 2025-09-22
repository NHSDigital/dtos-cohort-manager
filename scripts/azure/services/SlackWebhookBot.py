import logging
import requests
import os
import re

logger = logging.getLogger()
logger.setLevel(logging.INFO)

class SlackWebhookBot:
    def __init__(self, webhook_url: str, timeout: int = 15):
        self.webhook_url = webhook_url
        self.timeout = timeout
        self.headers = {
            'Content-Type': 'application/json',
        }

    def summarise_zap_report(self, content: str) -> str:
        """Extract a clean summary from a ZAP scan report text."""
        # Totals
        total_urls = re.search(r"Total of (\d+) URLs", content)
        totals_line = re.search(r"FAIL-NEW.*", content)

        # Warnings
        warn_matches = re.findall(r"WARN-NEW: (.+?)\s+\[.*?\]\s+x\s+\d+", content)
        warn_summary = "\n".join([f"- {w}" for w in warn_matches]) if warn_matches else "None"

        summary = (
            f"*ZAP Scan Summary:*\n"
            f"- URLs Scanned: {total_urls.group(1) if total_urls else 'N/A'}\n"
            f"- {totals_line.group(0) if totals_line else 'No totals found'}\n"
            f"- Warnings:\n{warn_summary}"
        )
        return summary

    def send_file_content(self, file_path: str, message: str = '') -> bool:
        if not os.path.isfile(file_path):
            logger.error(f'The file at path {file_path} does not exist.')
            return False

        try:
            with open(file_path, 'r') as file:
                file_content = file.read()

            summary = self.summarise_zap_report(file_content)

            payload = {
                "text": f"{message}\n{summary}"
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
