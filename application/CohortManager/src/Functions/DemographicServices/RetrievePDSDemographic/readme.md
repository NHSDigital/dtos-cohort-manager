# The Retrieve PDS Demographic function

The retrieve PDS demographic function is primarily used in the manual add process and as its name suggests gets any demographic data for some given NHS number. There is a second parameter and called "sourceFileName". in testing this can be just be set to some random string as its not really important but be aware that when this function is called by another function this parameter should be accordingly

to talk to any of the NHS PDS services we need to generate a Bearer token. We do this by using a a number of other smaller classes:

- AuthorizationClientCredentials
- JwtTokenService
- BearerTokenService

## BearerTokenService
In the `GetBearerToken` function we first check that there is not a valid token in the memory cache. We do this because whenever we get new bearer token it lasts for 10 minutes before we can't use it again.
if there is nothing in the memory cache we call the `AuthorizationClientCredentials` class

## AuthorizationClientCredentials

In here we:
- generate the JWT token and sign it with our private key
- assign the signed jwt token and other needed values to a dictionary that allows us to call the pds service
- check that the JWT is token is correct by calling PDS's oauth2 token service

This will allow us to check and make sure that the our signed token is correct.

## JwtTokenService 

in this class we generate our token after have signed it see ` SigningCredentialsProvider`. for more details on singing the key.
we use our client id, which you find in the NHS developer portal it is the API key. Take note that to use this function and not use the fake services you
need to:

1. Create a public private key pair in PEM format
2. upload your public key to the NHS developer portal under some new or existing application
3. make sure to make not of the client id in that application in the portal
4. if you are creating a new application the NHS dev portal you'll need to attach the PDS service

you'll need to make to set the audience as well which is just the same as the AuthTokenURL because we are going to call the AuthTokenURL to check that our signed token is correct and can be used
next we call the AuthTokenURL with our signed JWT and then return our access token that we get back from the response of the AuthToken service
then we back to the bearer token service, set the token to the memory and use it to call the PDS function

## SigningCredentialsProvider

this class is where get read in the private key and sign our token with it. first we sanitize our key this is so the we not have unwanted strings in our key. Then we create a byte array from our key. this is so that we van use it to create a security key.
you will notice that we need a key id this should be the name as the filename that key is. in azure make sure to keep note of the file name as whatever you call the file of the key will remain the key id.
next we return a signed version of our key to use to generate the JWT token from

## Main function body

the rest of the function is self explanatory. We call PDS we our token given that it is correct and return different status' depending on if the data exists in PDS. When the record does exist in pds we update our records and if they do not exist we do nothing to our
take note that if the record does exist in our database then we update our records so that all matches that means our cash is correct
Take not that if we get back a ConfidentialityCode of 'R' then means we cannot update our records and we must return not found. This is done because it means that record cannot be used by us
