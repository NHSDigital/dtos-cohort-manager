declare module "next-auth" {
  interface User {
    uid: string;
    firstName?: string;
    lastName?: string;
    sub?: string;
    sid?: string;
    odsCode?: string;
    orgName?: string;
    workgroups?: string[];
    workgroups_codes?: string[];
  }
}

export interface DecodedCIS2Token {
  iss: string;
  aud: string;
  idassurancelevel: string;
  authentication_assurance_level: string;
}
