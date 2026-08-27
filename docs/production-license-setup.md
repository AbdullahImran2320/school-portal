# Production license setup

The Bright Grammar School Portal client now uses a central license server for rental activation and periodic validation.

## Client API configuration

Edit `appsettings.json` before packaging a client build:

- `License:ServerUrl` = your HTTPS license-server URL, for example `https://license.example.com`
- `License:PublicKeyPem` = the PUBLIC key from the license server's `keys/public.pem`

Never put the license server's `keys/private.pem` in the client application.

## Trial

- 3 calendar months from first initialization.
- Warning begins in the final 30 days.
- The portal is blocked after expiry.

## Paid rental

A paid license is issued by the central license server and activated on one installation. The client stores a signed license token locally and validates it online at most once per day. A successful online validation grants a 14-day offline grace period.

## License server

The companion `LicenseServer` project is distributed separately. Deploy it on infrastructure you control. Keep its private signing key private.
