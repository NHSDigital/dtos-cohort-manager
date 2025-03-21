import { QueueClient } from '@azure/storage-queue';
import { expect, type Locator, type Page } from '@playwright/test';
import playwrightConfig from '../playwright.config';
import configJson from '../playwrightConfig.json'; // Adjust the path as necessary
export class EndToEndHelper {

    async GetCohortRecordsFromAPI(rowCount): Promise<[]> {
        try 
        {
            const response = await fetch(configJson.GetCohortDistributionURL+`?rowCount=${rowCount}`);
            if (response.ok) {
                var responseJson = await response.json();
                if (responseJson.length > 1) {
                return responseJson;
                }
                return responseJson[0];
            } else {
            console.error(`Error: ${response.status}`);
    
            return [];
            }
        }
        catch (error ) {
        return []
        }
    }


    async GetErrorsFromExceptionAPI(): Promise<[]> {
        var response = await fetch(configJson.GetValidationExceptionsURL) 
        try 
        {
            if (response.ok) {
                var responseJson = await response.json();
                let items = responseJson.Items
                return items;

            } else {
                console.error(`Error: ${response.status}`);
                return [];
            }
        }
        catch(error) {
            return [];
        }
    }

    async retrieveNextMessage(queueClient: QueueClient): Promise<boolean> {
        const exists = await queueClient.exists();
        if (exists) 
        {
            const properties = await queueClient.getProperties();

            if (properties.approximateMessagesCount && properties.approximateMessagesCount > 0) {
            const receiveResponse = await queueClient.receiveMessages({ numberOfMessages: 1 });

            if (receiveResponse.receivedMessageItems.length > 0) {
                
                const message = receiveResponse.receivedMessageItems[0];
                const messageText = message.messageText;

                await queueClient.deleteMessage(message.messageId, message.popReceipt);
                return messageText !== "";
                }
            }
        }
        return false;
    }

    delay(ms) {
        new Promise(res => setTimeout(res, ms));
    }
}