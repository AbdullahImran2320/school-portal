# Production license setup

This Angular application is the client UI for the licensed Bright Grammar School Portal.

## Client responsibilities

The frontend:
- reads license status from the local ASP.NET Core API
- displays the three-month trial status
- displays the final-30-day warning
- allows the customer to enter a rental license key
- displays the expired-license activation screen

## Security boundary

The Angular application MUST NOT contain:
- the LicenseServer private signing key
- the LicenseServer issuer secret
- LicenseServer administrator credentials
- LicenseServer source code

The backend is the authority for license enforcement. The frontend UI is not a security boundary.

## Production configuration

`environments/environment.production.ts` must point to the production local Bright Grammar School Portal API used by the installed application.

The central LicenseServer URL belongs in the ASP.NET Core backend configuration, not in Angular.

## Existing routes

- `/license` — license status and activation
- `/license/expired` — expired-license activation screen

Protected portal routes use the Angular license guard, while the ASP.NET Core backend independently enforces license expiry.
