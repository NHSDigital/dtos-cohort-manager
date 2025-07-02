import { expect, Locator,Page } from "@playwright/test";
import BasePage from "./basePage";


export class TermsCoditionsPage extends BasePage{
     readonly page:Page;
     readonly cisLink:Locator;
     readonly cisandnhstermsLink:Locator;
     readonly cookiesPolicyLink:Locator;

    constructor(page:Page){
        super(page)
        this.page = page;
        this.cisLink = page.locator('//a[normalize-space(text())="Care Identity Service (CIS)"]');
        this.cisandnhstermsLink = page.locator('//a[normalize-space(text())="CIS and NHS Spine terms and conditions"]')
        this.cookiesPolicyLink = page.locator('//a[normalize-space(text())="cookies policy"]')
    }

    

    async clickCISLink(){
        await this.clickElement(this.cisLink)
    }

    async clickCISandNHSLink(){
        await this.clickElement(this.cisandnhstermsLink)
    }

    async clickCookiesPolicyLink(){
        await this.clickElement(this.cookiesPolicyLink)
    }

}