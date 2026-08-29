# Changelog

All notable changes to Hardware Store Portal are documented here.

Format loosely follows [Keep a Changelog](https://keepachangelog.com/).
Each entry should match the `AppVersion` set in `installer.iss` at the time it was built.

## [Unreleased]
<!-- List changes here as you make them, then move them under a new
     version heading (like the ones below) once you build and ship
     that installer. -->

## [1.0.0] - 2026-08-29

### Added
- Categories as a first-class entity — admin-only add/delete, dropdown on the
  Add/Edit Product form, server-side protection against deleting a category
  still assigned to a product
- Clean repository structure: `Backend/` + `Frontend/` split, `build.bat` →
  `compile-installer.bat` pipeline, `docs/`, `LICENSE`, `.gitignore`,
  `.gitattributes`, basic CI workflow

### Fixed
- Login failure on installed builds — backend was using SQL Server LocalDB,
  which isn't portable to end-user machines; migrated to SQLite with a
  ProgramData-based DB path, automatic `Database.Migrate()`, and automatic
  seeding of the `admin` / `Muneeb` / `Shahid` accounts on first run
- Login failure specifically on **production installer** builds — the
  frontend's `apiUrl` was hardcoded to the Visual Studio dev port instead of
  a relative path; added `environment.prod.ts` (`apiUrl: '/api'`) with a
  `fileReplacements` entry in `angular.json` so production builds always
  target the correct origin regardless of port
- Decimal precision warnings on `Bill`, `BillItem`, `Payment`, and `Product`
  fields under SQLite — added explicit `HasPrecision(18, 2)` in
  `AppDbContext.OnModelCreating`

---

<!--
TEMPLATE FOR THE NEXT RELEASE — copy this block, fill it in, and move it
above this comment (keep newest version at the top) when you ship a new
installer:

## [x.y.z] - YYYY-MM-DD

### Added
- 

### Changed
- 

### Fixed
- 
-->
