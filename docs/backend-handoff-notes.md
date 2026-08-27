Bright Grammar School Portal Backend handoff

Baseline: SchoolPortal_API_DASHBOARD.zip (latest backend/dashboard build available in this session).

Known state:
- Backend is working with the frontend dashboard.
- Dashboard summary endpoint is functional in the current working state.
- The earlier SQLite decimal SUM translation issue should not be reintroduced.

Use this package as the backend baseline when handing the project to another agent.


LICENSE FEATURE (updated 2026-08-27 — real activation flow)
- 3-calendar-month free trial created on first API startup, tied to a per-machine InstallationId.
- Final 30 days show a warning in Angular.
- Expired protected API calls return HTTP 403 LICENSE_EXPIRED.
- Angular license guard redirects expired users to /license/expired.
- /license shows status and activation UI.
- POST /api/license/activate is Admin-only. It calls the central LicenseServer's
  /api/client/licenses/activate with the entered key + this installation's ID, then
  verifies the RSA-signed license token returned locally against License:PublicKeyPem
  before accepting it. There is no local placeholder key prefix anymore — activation
  always requires a real signed response from LicenseServer.
- GetStatusAsync re-validates online against LicenseServer at most once per day and
  allows a 14-day offline grace period if the server can't be reached.
- License:ServerUrl is currently blank in this deliverable, so the app runs in
  trial-only mode until LicenseServer is deployed and that URL is set (see
  PRODUCTION_LICENSE_SETUP.md).
- Sidebar school logo markup/styles were corrected to use /school-logo-transparent.png.
