Usage example:
(ExternalSendPaymentExample.exe|run_on_mono.sh) --host="127.0.0.1" --host-port=14300 --username="testUser" --password="testPassword" --wallet-from="00000000-0000-0000-0000-000000000000" --wallet-to="00000000-0000-0000-0000-000000000000" --amount=1 --hmac-key-b64="hmac_base64" [--anonymous=1] [--comment-bytes-b64="SGVsbG8gd29ybGQgcGF5bWVudA=="] [--request-guid="00000000-0000-0000-0000-000000000000"]

SGVsbG8gd29ybGQgcGF5bWVudA== equals b64(utf8bytes("Hello world payment"))

If AddressAccessDeniedException error message shows on rpc server starting 
with Windows OS run
"netsh http add urlacl url=http://localhost:14300/ user=WINDOWS_USERNAME"
with cmd.exe (run as admin)

Dependencies:
* BasicAuthHttpJsonRpcLib
* BtmGeneralClientInterfacesLib
* MiscUtilsLib