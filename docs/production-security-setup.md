# Production security setup

## JWT signing key

`appsettings.json` ships with a public placeholder value for `Jwt:Key`.
It must never be used on a real installation.

On startup `Program.cs` checks the configured key. If it is missing, shorter
than 32 bytes, or still starts with `REPLACE_WITH_YOUR_OWN_SECRET`, the app
creates a random key once, saves it as `jwt.key` in the same folder as the
database (`%ProgramData%\BrightGrammarSchoolPortal\` once installed), and reuses
it on every later start.

- Setting a real `Jwt:Key` in configuration takes priority.
- Deleting `jwt.key` creates a new key and signs everyone out once.
- Never commit `jwt.key`; it is in `.gitignore`.

## Default accounts

A fresh install creates `admin`, `accountant` and `teacher` with well-known
default passwords. Change all three on installation day (Admin → Users, or the
change-password page).

## First-run data

- Challan Settings start as "Set in Admin > Challan Settings" placeholders.
  Enter the school's account title, bank, account number and payment terms.
- A sample parent record is created in Development only.

## Data that must stay out of the repository

Student photos (`Backend/StudentPhotos/`), the database (`*.db`), backups
(`Backups/`), `jwt.key` and `*.sqbpro` are all in `.gitignore`. Backups cover the
database only, not student photos; copy both folders to external storage
regularly.
