# Bright Grammar Roll Number implementation

## Format

`{Prefix}{2-digit admission year}{ClassCode}{padded sequence}`

Example: prefix `F`, admitted 2026, class code `NRA`, position 1, 2 digits → `F26NRA01`.

| Part | Where it comes from |
| --- | --- |
| Prefix | Admin → Roll Number Settings (migration seeds `R`) |
| Year | The last two digits of the student's own admission date |
| Class code | Admin → Classes → the section's "Roll No. Code" (unique per section) |
| Sequence | The student's position in that section, padded to "Sequence Digits" (migration seeds 3) |

Keep class codes short and include the section letter, but not the year
(the year is added automatically).

## Behaviour

- A class/section must have a code before its students can get roll numbers.
  "Assign Roll Numbers" skips students whose class has no code and says so.
- The stored number and the position (`RollNumberSequence`) are separate
  columns. Positions never change when the format changes.
- **Saving Roll Number Settings** rebuilds every existing roll number.
  **Editing a class code** rebuilds that class's roll numbers.
  Both use `Services/RollNumberRebuilder.cs`.
- Changing a student's position (`SetRollNumberAsync`) shifts classmates like
  moving an item in a list. It runs in two phases inside a transaction because
  `Students.RollNumber` has a UNIQUE index that SQLite checks after every update.
- Moving a student to another class, or promoting them, clears their roll
  number and position. Issue a new one with "Assign Roll Numbers".

## Backend files

- `Services/StudentService.cs` — create, format (`BuildRollNumberCode`), reorder
- `Services/RollNumberRebuilder.cs` — rebuilds existing numbers from position
- `Services/PromotionService.cs` — clears roll numbers on promotion
- `Controllers/SettingsController.cs` — `PUT /api/settings/roll-number`
- `Controllers/ClassController.cs` — `PUT /api/classes/{id}/code`
- `Controllers/StudentController.cs` — assign missing roll numbers, delete photo on delete
- `Repositories/StudentRepositories.cs` — student list order: class order,
  section, position, name

## Frontend files

- `features/admin/roll-number-settings/` — prefix and digits form
- `features/students/student-list/` — roll number column, position input,
  "Assign Roll Numbers" and its result message

## Database and migrations

- `AddRollNumberColumnFix` adds `Students.RollNumber` and its unique index.
- `AddRollNumberFormatting` adds `Classes.ClassCode`, `Students.RollNumberSequence`
  and the `RollNumberSettings` table (seeded `R`, 3). It changes `RollNumber`
  to text and **clears every existing roll number** (clean cutover).
  It has no `.Designer.cs`, so column drops use raw
  `ALTER TABLE ... DROP COLUMN` (EF's SQLite provider cannot emit
  `DropColumnOperation` without a target model). SQLite 3.35+ is required,
  which .NET 8 bundles.
- `AddClassManagement` uses `IF NOT EXISTS` so older installs that already
  have `LicenseInfos` upgrade cleanly.

## Upgrading an existing school

1. Back up `SchoolPortal.db` and test on a copy first.
2. Install the new build. The migration clears all roll numbers once.
3. Set a code for every class/section in Admin → Classes.
4. Check the prefix and digits in Admin → Roll Number Settings.
5. Students → "Assign Roll Numbers".
