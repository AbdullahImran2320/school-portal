# Bright Grammar School Portal — Default Login Credentials

These are the accounts seeded automatically the first time the backend runs
on an empty database (see `Program.cs`). They exist purely so a fresh
install is usable immediately.

| Username     | Password         | Role       |
|--------------|------------------|------------|
| `admin`      | `Admin@123`      | Admin      |
| `accountant` | `Accountant@123` | Accountant |
| `teacher`    | `Teacher@123`    | Teacher    |

## Before any real deployment

**Change every one of these passwords before handing the portal to the
school.** They are visible in plain text in the project's source code and
README, so anyone with access to the repo already knows them.

To change a password:
1. Log in with the account above.
2. Go to **Admin → Users** (Admin account only) to manage roles, or use
   the account's own profile/change-password screen if the app has one.
3. If no in-app "change password" screen exists yet for a given role,
   the password hash can be reset directly by re-registering the account
   or updating it via the database — ask me if you'd like help adding a
   proper change-password screen instead.

## Note on the Principal / Admin question

There's currently no separate "Principal" role in the system — whoever
holds the `admin` login has full administrative access (including the new
Manage Classes screen). If the school wants the principal and a separate
day-to-day admin to have different permission levels later, that would need
a new role added to the system (`UserRole` enum on the backend).
