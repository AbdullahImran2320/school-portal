# Changelog

All notable changes to the Bright Grammar School Portal are documented here.

Format loosely follows [Keep a Changelog](https://keepachangelog.com/).
Each entry should match the `AppVersion` set in `installer.iss` at the time it was built.

## [Unreleased]
<!-- Move these under a new version heading (e.g. "## [1.1.0] - YYYY-MM-DD")
     once you build and ship the installer. -->

### Added
- **Formatted roll numbers** — `{Prefix}{2-digit admission year}{ClassCode}{sequence}`,
  e.g. `F26NRA01`. Prefix and sequence digits are set in Admin → Roll Number
  Settings; each class/section has its own code in Admin → Classes. See
  `docs/roll-number-implementation.md`.
- Roll numbers refresh automatically: saving Roll Number Settings rebuilds every
  existing roll number, and editing a class code rebuilds that class's roll
  numbers. Each student keeps their position in the class.
- Per-installation JWT signing key: generated on first start and stored as
  `jwt.key` next to the database when `Jwt:Key` is missing, too short, or still
  the placeholder. See `docs/production-security-setup.md`.

### Changed
- The Students list is now ordered by class (Playgroup, Nursery, Prep, Class 1 …),
  then section, then roll number position, then name. Previously it followed
  insertion order, so bulk-imported students all appeared at the bottom.
- Promoting a student to the next class now clears their roll number and
  position (they belong to the old class register); issue new ones with
  "Assign Roll Numbers".
- Deleting a student also deletes their photo file.
- "Assign Roll Numbers" no longer says "Everyone already has a roll number"
  when students were skipped because their class has no roll number code.
- A fresh install no longer seeds another school's bank details: Challan
  Settings start as "Set in Admin > Challan Settings" placeholders. The sample
  parent record is created in Development only.
- Upgrade note: the `AddRollNumberFormatting` migration clears every existing
  roll number once (clean cutover) and sets each class's code to blank. Set
  class codes in Admin → Classes, then click "Assign Roll Numbers".

### Fixed
- Startup crash on upgrade from older installs: `SQLite Error 1: table
  "LicenseInfos" already exists`. The `AddClassManagement` migration is now
  idempotent (`IF NOT EXISTS`).
- Editing a student's roll number position could fail with `UNIQUE constraint
  failed: Students.RollNumber`; reordering now runs in two phases inside a
  transaction.
- `AddRollNumberFormatting` no longer throws `SQLite does not support this
  migration operation ('DropColumnOperation')`; column drops use SQLite's native
  `DROP COLUMN` through raw SQL.

### Security
- The installed app no longer depends on the public placeholder JWT key from
  `appsettings.json` (see Added).
- Removed a real bank account number from the source-controlled seed data.
- `.gitignore` now excludes `StudentPhotos/`, `Backups/`, `jwt.key` and
  `*.sqbpro`.
