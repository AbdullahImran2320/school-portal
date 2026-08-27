# Bright Grammar School Portal Licensing

This build adds a local 3-calendar-month trial and a one-month rental activation flow.

## API
- `GET /api/license/status` — current trial/license state.
- `POST /api/license/activate` — Admin-only activation.

## Trial
- Starts automatically on first backend startup.
- Lasts three calendar months.
- Warning is shown during the final 30 days.
- After expiry, protected API calls return HTTP 403 with `code: LICENSE_EXPIRED`.

## Activation (real flow)
`POST /api/license/activate` (Admin-only) sends the entered key plus this installation's
ID to the central LicenseServer's `/api/client/licenses/activate`. LicenseServer returns
an RSA-signed license token; `LicenseService` verifies that signature locally against
`License:PublicKeyPem` before marking the install activated. Nothing is trusted without
a valid signature — there is no placeholder key prefix in this build. Status checks
re-validate against LicenseServer at most once a day, with a 14-day offline grace
period if the server is unreachable. See PRODUCTION_LICENSE_SETUP.md for the server-side
half of this flow, and keep the license-issuing private key (`private.pem`) off the
client machine entirely — only the public key belongs in this app's appsettings.json.

The license table is created idempotently at startup so existing SQLite installations can receive licensing without a developer running EF CLI.
