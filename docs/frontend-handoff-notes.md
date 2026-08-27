Bright Grammar School Portal Frontend handoff

Baseline: SchoolPortal_frontend_DASHBOARD.zip (latest working dashboard build available in this session).

Latest known working state:
- Angular portal/dashboard loads and displays dashboard data.
- Dashboard API path uses environment.apiUrl + /dashboard.
- Sidebar logo was the remaining visual issue.
- sidebar.component.html now uses /school-logo-transparent.png and the brand-mark class.
- sidebar.component.scss styles the actual logo image.
- Logo asset is included at public/school-logo-transparent.png.

Do not revert the dashboard service/environment changes from this baseline.

PRODUCTION SERVING (2026-08-27)
- environment.production.ts apiUrl changed from https://localhost:7247/api to the
  relative path /api — the Angular build is now served same-origin as static files
  from the ASP.NET Core backend's wwwroot, so no absolute dev-server URL is needed
  and no CORS is involved in production.

LICENSE FEATURE (updated 2026-08-27 — real activation flow)
- 3-calendar-month free trial created on first API startup.
- Final 30 days show a warning in Angular.
- Expired protected API calls return HTTP 403 LICENSE_EXPIRED.
- Angular license guard redirects expired users to /license/expired.
- /license shows status and activation UI.
- POST /api/license/activate is Admin-only. There is no local placeholder key prefix
  (the earlier BAY- prefix shortcut is gone) — activation always requires a real
  RSA-signed response from the central LicenseServer, verified by the backend against
  License:PublicKeyPem before it's accepted.
- Sidebar school logo markup/styles were corrected to use /school-logo-transparent.png.
