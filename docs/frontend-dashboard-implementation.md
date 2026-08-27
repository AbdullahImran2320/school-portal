# Bright Grammar Dashboard implementation

## Frontend changes

- Replaced the placeholder dashboard with a real Angular standalone dashboard.
- Added `app/features/dashboard/models/dashboard.models.ts`.
- Added `app/features/dashboard/services/dashboard.service.ts`.
- Added responsive KPI cards for:
  - Total Active Students
  - Fee Challans generated this month
  - Fee Amount Collected this month
- Added today's class-wise attendance table.
- Added a school logo watermark to the dashboard hero.
- Added the school emblem to the top-right topbar and sidebar brand.
- Added:
  - `public/school-logo.png`
  - `public/school-logo-mark.png`

The logo is generated from the supplied Bright Grammar image with the checkerboard background removed.

## API contract

The dashboard calls:

`GET /api/dashboard/summary`

The endpoint is authorized for `Admin`, `Accountant`, and `Teacher`.
