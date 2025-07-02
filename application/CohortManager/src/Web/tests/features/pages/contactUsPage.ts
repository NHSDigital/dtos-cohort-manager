import { expect, Locator,Page } from "@playwright/test";
import BasePage from "./basePage";


export class ContactusPage extends BasePage{
     readonly page:Page;
     readonly technicalsupportLink:Locator;
     readonly rapidincidentLink:Locator;

    constructor(page:Page){
        super(page)
        this.page = page;
        this.technicalsupportLink = page.locator('//a[normalize-space(text())="technical support and general enquiries"]');
        this.rapidincidentLink = page.locator('//a[normalize-space(text())="report an incident"]')
    }

    

    async clickTechnicalLink(){
        await this.clickElement(this.technicalsupportLink)
    }

    async clickRapidincidentLink(){
        await this.clickElement(this.rapidincidentLink)
    }


 

}