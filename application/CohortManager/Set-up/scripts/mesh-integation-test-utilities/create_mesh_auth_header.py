""" Python code to generate a valid authorization header. """
import hmac
import uuid
from datetime import datetime, timezone
from hashlib import sha256

AUTH_SCHEMA_NAME = "NHSMESH "  # Note: Space at the end of the schema.
SHARED_KEY = "<SharedKey>"  # Note: Don't hard code your passwords in a real implementation.


def build_auth_header(mailbox_id: str, password: str = "password",  nonce: str = None, nonce_count: int = 0):
    """ Generate MESH Authorization header for mailboxid. """
    # Generate a GUID if required.
    if not nonce:
        nonce = str(uuid.uuid4())
    # Current time formatted as yyyyMMddHHmm
    # for example, 4th May 2020 13:05 would be 202005041305
    timestamp = datetime.now(timezone.utc).strftime("%Y%m%d%H%M")

    # for example, NHSMESH AMP01HC001:bd0e2bd5-218e-41d0-83a9-73fdec414803:0:202005041305
    hmac_msg = mailbox_id + ":" + nonce + ":" + str(nonce_count) + ":" + password + ":" + timestamp

    # HMAC is a standard crypto hash method built in the python standard library.
    hash_code = hmac.HMAC(SHARED_KEY.encode(), hmac_msg.encode(), sha256).hexdigest()
    return (
            AUTH_SCHEMA_NAME # Note: No colon between 1st and 2nd elements.
            + mailbox_id + ":"
            + nonce + ":"
            + str(nonce_count) + ":"
            + timestamp+ ":"
            + hash_code
    )


# example usage
MAILBOX_ID = "<MailboxId>" # Note: Don't hard code your mailbox id in a real implementation.
MAILBOX_PASSWORD = "<MailBoxPassword>"  # Note: Don't hard code your passwords in a real implementation.

# send a new nonce each time
print(build_auth_header(MAILBOX_ID, MAILBOX_PASSWORD))

# # or reuse the nonce and increment the nonce_count
# my_nonce = str(uuid.uuid4())

# print(build_auth_header(MAILBOX_ID, MAILBOX_PASSWORD, my_nonce, nonce_count=1))
# print(build_auth_header(MAILBOX_ID, MAILBOX_PASSWORD, my_nonce, nonce_count=2))
