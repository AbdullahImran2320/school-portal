# Bright Grammar School Portal

A full-stack school administration portal — student records, class/section
management, monthly fee ledgers, challans, receipts, concessions, fines,
attendance, academics, audit logging, and automated backups — built as a
single self-contained Windows desktop application.

**Stack:** Angular 20 (standalone components, signals) · ASP.NET Core 8 Web API ·
Entity Framework Core · SQLite · JWT authentication · xUnit

---

## Features

- **Students & Classes** — admission records, B-Form/parent details,
  admin-managed classes and sections (add/delete a class, add/delete a
  section, manage the reusable section-label list), bulk student import from
  Excel for onboarding an already-running school, year-end promotion
  (matches students to same-named sections automatically, flags anything
  ambiguous for manual review instead of guessing).
- **Fees** — monthly fee grid per class, one-off charges (admission/exam/stationery
  fees), student-level concessions, automatic + manual late fines, defaulters
  report, monthly collection summary by class.
- **Challans & Receipts** — three-copy printable fee challans (Bank/School/Parent)
  and paid receipts matching the school's paper format, with bank details and
  payment terms editable from Admin → Challan Settings — no redeploy needed to
  change them. Challans can be filtered to a specific charge type or admission
  date range.
- **Reports** — Excel export (.xlsx) for the defaulters list and the monthly
  collection summary.
- **Academics** — subjects, exams, result entry, report cards with pass/fail logic.
- **Attendance** — daily marking per class, per-student attendance history/report.
- **Dashboard** — enrollment, fee collection, and today's attendance at a glance.
- **Audit Log** — every create/update/delete across the system is automatically
  recorded with the acting user, timestamp, and field-level changes.
- **Backups** — a backup is created automatically once a day when the portal
  starts, using SQLite's own backup API (not a plain file copy, so a backup
  never misses data sitting in an unflushed write-ahead log). The last 30 are
  kept. Admin → Backups also allows a manual "Backup Now" and downloading any
  past backup.
- **Role-based access** — Admin / Accountant / Teacher, with self-registered
  accounts starting as `Pending` (no access) until an Admin approves them.
  Any logged-in user can change their own password from the lock icon in the
  top bar. Five failed login attempts locks the account for 15 minutes
  (configurable in `appsettings.json` under `LoginSecuritySettings`).
- **Licensing** — built-in trial/activation flow against a separate, privately
  hosted License Server (not part of this repo — see [Licensing](#licensing) below).

## Architecture

```
Bright Grammar School Portal/
├─ Backend/       ASP.NET Core 8 Web API (SchoolPortal.API) — open in Visual Studio
├─ Backend.Tests/ xUnit tests for the fee-calculation and promotion logic
├─ Frontend/      Angular 20 standalone app — open in VS Code
├─ docs/          Implementation notes for the dashboard and licensing subsystems
├─ build.bat            Builds Frontend + Backend into /publish
├─ compile-installer.bat  Compiles the Windows installer from /publish
├─ installer.iss         Inno Setup script
└─ LaunchBrightGrammarSchoolPortal.vbs  Installed shortcut launcher
```

In production, the compiled Angular app is served as static files directly from the
ASP.NET Core backend (`wwwroot`) — one process, one port (`http://localhost:5000`),
no separate web server. In development, the Angular dev server (`ng serve`, port
4200) talks to the API over CORS instead — browsing to `localhost:5000` directly
during development won't work, since `wwwroot` only exists after `build.bat` runs.

## Getting started (development)

**Prerequisites:** [.NET 8 SDK](https://dotnet.microsoft.com/download), [Node.js](https://nodejs.org) (LTS), npm.

### Backend

```bash
cd Backend
dotnet restore
dotnet run
```

The API listens on `http://localhost:5000` and applies EF Core migrations
automatically on startup. A local `SchoolPortal.db` SQLite file is created next to
the executable, seeded with default classes, section labels, fee components,
challan settings, and one login per role:

| Username     | Password        | Role       |
|--------------|-----------------|------------|
| `admin`      | `Admin@123`     | Admin      |
| `accountant` | `Accountant@123`| Accountant |
| `teacher`    | `Teacher@123`   | Teacher    |

> **Change these before any real deployment.** They exist purely so a fresh
> checkout is usable immediately. Once logged in, use the lock icon in the top
> bar to set a real password for each account.

Before running for real, replace the placeholder in `Backend/appsettings.json`:

```json
"Jwt": { "Key": "REPLACE_WITH_YOUR_OWN_SECRET_AT_LEAST_32_BYTES_BASE64" }
```

Generate a real one, e.g.:

```bash
# any 32+ byte random value, base64-encoded, works
openssl rand -base64 48
```

### Frontend

```bash
cd Frontend
npm install
npm start        # ng serve, http://localhost:4200
```

The dev environment (`Frontend/environments/environment.ts`) points at
`http://localhost:5000/api`.

### Tests

```bash
cd Backend.Tests
dotnet test
```

Covers `FeeCalculator` (late fees, discounts, admission-month billing rules)
and `PromotionService` (section matching, graduation, hold-back, and safety
against running promotion twice on the same year).

## Building the Windows installer

**Additional prerequisite:** [Inno Setup 6](https://jrsoftware.org/isinfo.php).

```
1. Run build.bat            → builds Angular (production) + publishes the API
                               self-contained (win-x64) into /publish
2. Run compile-installer.bat → compiles installer.iss with Inno Setup
                               → output/BrightGrammarSchoolPortal_Setup_<version>.exe
```

`build.bat` fails fast and prints a clear `[ERROR]` at whichever stage breaks —
frontend build, backend publish, or missing output files — instead of silently
continuing.

### Runtime paths

- **App URL:** `http://localhost:5000`
- **Database:** `%ProgramData%\BrightGrammarSchoolPortal\SchoolPortal.db`
  (installer rewrites the connection string to this path on install, so
  reinstalling/upgrading never touches or deletes existing school data)
- **Backups:** `%ProgramData%\BrightGrammarSchoolPortal\Backups\` — created
  automatically; also downloadable from Admin → Backups inside the app.
- **Launch:** the Start Menu/Desktop shortcuts run
  `LaunchBrightGrammarSchoolPortal.vbs`, which starts the backend and opens the
  default browser automatically, and won't start a second copy if one is already
  running.

## Licensing

This app includes a trial/license-check middleware (3-month free trial, then
activation against a License Server). **The License Server itself — its source,
signing keys, and admin secrets — is a separate, privately-hosted project and is
intentionally not part of this repository.** `Backend/appsettings.json` ships
with `License:ServerUrl` empty, so a fresh checkout simply runs in trial mode.
See `docs/backend-license-implementation.md` for how the client-side check works,
and `docs/production-license-setup.md` for what needs to be configured before a
real deployment that requires license activation.

## Documentation

- [`docs/backend-dashboard-implementation.md`](docs/backend-dashboard-implementation.md) /
  [`docs/frontend-dashboard-implementation.md`](docs/frontend-dashboard-implementation.md)
- [`docs/backend-license-implementation.md`](docs/backend-license-implementation.md) /
  [`docs/frontend-license-implementation.md`](docs/frontend-license-implementation.md)
- [`docs/production-license-setup.md`](docs/production-license-setup.md)
- [`docs/backend-handoff-notes.md`](docs/backend-handoff-notes.md) /
  [`docs/frontend-handoff-notes.md`](docs/frontend-handoff-notes.md)

## Backup

Automated daily backups are built into the app itself — see
[Features](#features) and [Runtime paths](#runtime-paths) above. As a manual
fallback, the entire application state still lives in one file:
`%ProgramData%\BrightGrammarSchoolPortal\SchoolPortal.db`, safe to copy while
the app is closed, or via the SQLite online backup API while it's running.

## License

Proprietary — developed by Rana Abdullah for Bright Grammar School. All rights
reserved unless a license is granted separately. See [`LICENSE`](LICENSE) for
the full terms.