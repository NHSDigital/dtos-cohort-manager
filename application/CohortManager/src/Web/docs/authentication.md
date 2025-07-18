# Authentication

We use [NHS CIS2 Authentication](https://digital.nhs.uk/services/care-identity-service/applications-and-services/cis2-authentication), which is the secure authentication service used by health and care professionals in England to access national clinical information systems. Users in the live environment use Smartcards to access the service, in development and testing environments we can use Microsoft Authenticator.

We use the [Auth.js library](https://authjs.dev/) to handle the authentication logic (`lib/auth.ts`). As well as handling the basic OAuth, there are a number of checks that are required by the CIS2 team. These are to check the legitimacy of the token and to ensure the correct assurance levels are met on the user account. There is also logic to handle session management and inactivity timeout.

The service _must_ use https, this is configured automatically in hosted environments but run with the following in development:

```bash
pnpm dev:secure
# or
npm dev:secure
```

> These are requirements of the NHS CIS2 contractual agreement and must not be altered unless told to do so.
> More information can be found in the [NHS2 CIS2 detailed guidance section](https://digital.nhs.uk/services/care-identity-service/applications-and-services/cis2-authentication/guidance-for-developers/detailed-guidance).

## Environment configuration

There are a number of `.env` variables that are required to configure the authentication part of the service.

```text
# Next Auth
NEXTAUTH_URL=https://localhost:3000/api/auth
NEXTAUTH_SECRET={RANDOM_SECRET_STRING}

# Required for hosted environments
AUTH_TRUST_HOST=true

# CIS2 Auth
AUTH_CIS2_ISSUER=https://am.nhsint.auth-ptl.cis2.spineservices.nhs.uk
AUTH_CIS2_CLIENT_ID={CLIENT_ID}
AUTH_CIS2_CLIENT_SECRET={CLIENT_SECRET}
```

`NEXTAUTH_URL` - URL is dependant on the environment you are in

`NEXTAUTH_SECRET` - you can use `openssl rand -base64 32` or [https://generate-secret.vercel.app/32](https://generate-secret.vercel.app/32) to generate a random value.

`AUTH_CIS2_ISSUER` - URL is dependant on the environment you are in

`AUTH_CIS2_CLIENT_ID` - Client ID for the Cohort Manager CIS2 account

`AUTH_CIS2_CLIENT_SECRET` - Client secret for the Cohort Manager CIS2 account

## Getting access to use NHS CIS2 authentication

To be able to use NHS CIS2 authentication you need to have a `uid` (12 digit identifier uniquely identifying the End-User within NHS CIS2 Authentication) created which can be bound to different [authenticator options](https://digital.nhs.uk/services/care-identity-service/applications-and-services/cis2-authentication#authenticator-options).

Your `uid` will need to be setup with the ODS code `X26` (NHS England) and have the standard role on integration and live envrionments.

For this service we recommend Smartcard and Microsoft Authenticator. Microsoft Authenticator only requires the app on a mobile device, but Smartcards require a little more setup.

### Smartcards

Smartcards require both hardware and software in order to use:

- Smartcard (Hardware)
- Smartcard reader (Hardware)
- Windows OS (Software)
- Identity Agent (Software)
- IA Registry Editor Tool (Software)
- Connected to the HSCN network either physically or on the VPN

> You need to make sure that your `uid` is bound to the authentication method and environment you want to use and that you're pointing your machine at the correct environment using the IA Registry Editor Tool.

## Authenticating as a test user in development

NHS CIS2 authentication does not provide a method to stub login in development or test environments, you need to use the above methods in order to log in.

Therefore in development and test environments you can provide basic credentials to log in, you can enter any email address and password to be authorised a session in order to run tests or make changes.

## Role based access controls (RBAC)

As well as having Authentication to provide access to the service, we limit access to the service using Role based access controls (RBAC) (`lib/checkAccess.ts`).

You need to add a comma separated list of `workgroup_codes` values of the users you want to be able to able access the Cohort Manager service, to `COHORT_MANAGER_RBAC_CODE` in the `.env` file.

```text
# CIS2 RBAC
COHORT_MANAGER_RBAC_CODE=000000000000
```
