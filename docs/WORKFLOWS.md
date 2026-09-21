# MEACH — User workflows

Complete guide to every workflow in the MEACH desktop application.

Currency is **Malagasy Ariary (MGA)**, shown with the symbol **Ar** (configurable in Settings).

---

## Table of contents

1. [First launch & login](#1-first-launch--login)
2. [Roles & what each role can do](#2-roles--what-each-role-can-do)
3. [Shell (main window)](#3-shell-main-window)
4. [Master data setup (recommended order)](#4-master-data-setup-recommended-order)
5. [Students](#5-students)
6. [Attendance](#6-attendance)
7. [Billing (creating fee obligations)](#7-billing-creating-fee-obligations)
8. [Collecting payments & receipts](#8-collecting-payments--receipts)
9. [Unpaid balances & payment history](#9-unpaid-balances--payment-history)
10. [Financial reports](#10-financial-reports)
11. [Administration](#11-administration)
12. [Business rules cheat sheet](#12-business-rules-cheat-sheet)

---

## 1. First launch & login

### What is created automatically

On the **first launch**, MEACH creates a local SQLite database and seeds only what is required to sign in and use the menus:

| Seeded | Purpose |
|--------|---------|
| Roles | Administrator, Accountant, Cashier, School manager |
| Admin user | Username `admin` |
| Academic levels | L1, L2, L3 |
| Payment types | Droit, Écolage, Livre, Mock Exam, Official Exam |
| School settings | Default school name, MGA / Ar, receipt footer |
| Current school year | Derived from today’s date (Sept–June) |

**No students, groups, fees, payments, or demo users** are created in a Release build.

> If you previously ran a Debug build with demo data, delete the existing database so Release starts clean:  
> `%LocalAppData%\SchoolManagement\school_management.db`

### Sign in

1. Start MEACH.
2. Enter username and password.
3. Click **Sign in**.

| Field | Default administrator |
|-------|------------------------|
| Username | `admin` |
| Password | `ChangeMe123!` |

### Mandatory password change

The first time `admin` signs in (or after an administrator resets a password with “must change”), a dialog **Choose a new password** appears.

1. Enter the current password.
2. Enter a new password (at least 8 characters, with at least one letter and one digit).
3. Confirm and save.

You cannot use the app until the password is changed. Cancelling signs you out.

### Optional password change

From the shell: **Change password** (same rules; not mandatory).

### Sign out

Shell → **Logout** → returns to the sign-in window.

---

## 2. Roles & what each role can do

| Capability | Administrator | Accountant | Cashier | School manager |
|------------|:-------------:|:----------:|:-------:|:--------------:|
| Dashboard | ✓ | ✓ | ✓ | ✓ |
| Students (view) | ✓ | ✓ | ✓ | ✓ |
| Students (create / edit / transfer / archive) | ✓ | | | ✓ |
| Academic levels & groups & schedules | ✓ | | | ✓ |
| School years (manage) | ✓ | view | | view |
| Attendance | ✓ | | | ✓ |
| View fees | ✓ | ✓ | ✓ | ✓ |
| Create bills (generate fees) | ✓ | ✓ | | |
| Register payments | ✓ | | ✓ | |
| Cancel / reverse payments | ✓ | | | |
| Receipts (print) | ✓ | view | ✓ | view |
| Financial reports | ✓ | ✓ | | ✓ |
| Users, payment types, settings, audit | ✓ | | | |

Menus only appear if your role has permission for that screen.

---

## 3. Shell (main window)

| Control | Action |
|---------|--------|
| Left navigation | Open a module (grouped: Overview, People, Attendance, Payments, Reports, Administration) |
| **Register a payment** | Opens the payment dialog (Cashier, Administrator) |
| **Refresh** | Reloads the current screen |
| **Change password** | Change your own password |
| **Logout** | End the session |
| School year label | Shows the **current** school year (or a warning if none) |

Window title: **MEACH**.

---

## 4. Master data setup (recommended order)

Do this once when commissioning a school PC:

```text
Settings → School years → Academic levels → Student groups (+ schedules) → Payment types (if needed) → Students
```

### Settings (Administration → Settings)

**Who:** Administrator  

1. Open **Settings**.
2. Set school name (required on receipts), address, phone, email, website.
3. Confirm currency code **MGA** and symbol **Ar** (or change if needed).
4. Set receipt footer and default due day (1–28).
5. Optionally browse for a logo.
6. **Save**.

**Backup database:** choose a destination file; MEACH writes a copy of the SQLite database.  
**Refresh overdue:** recomputes overdue status on unpaid fees.

### School years (People → School years)

**Who:** View — Administrator, Accountant, School manager. Manage — Administrator only.

1. **Create** a school year (name, start/end dates).
2. **Set current** on the year that is in use (only open years).
3. When the year ends, **Close** it (blocks new billing and payments for students on that year).
4. **Reopen** if you need to correct data.

### Academic levels (People → Academic levels)

**Who:** School manager, Administrator  

1. Review seeded **L1 / L2 / L3**, or create more levels.
2. Select a level to see its groups.
3. **Edit**, **Activate**, or **Deactivate** as needed.

### Student groups (People → Student groups)

**Who:** School manager, Administrator  

1. Filter by level / active / search.
2. **Create** a group (name, academic level, optional description).
3. Select the group to see enrolled students and the **schedule**.
4. **Schedules:** **Add** / **Edit** / **Remove** sessions (weekday + start/end time). End must be after start.
5. **Deactivate** or **Archive** a group (archive only if it has no students).

Schedules drive **Daily attendance** (a session must exist for that weekday).

### Payment types (Administration → Payment types)

**Who:** Administrator  

Built-in types (seeded): **Droit**, **Écolage**, **Livre**, **Mock Exam**, **Official Exam**.

1. **Create** custom types (e.g. Excursion, Trip): name, description, default amount, frequency (**OneTime** or **Monthly**).
2. Prefer **OneTime** for types billed under **Other fees**.
3. **Edit** amounts and descriptions; **deactivate** instead of deleting if the type was already used.

Custom types appear under **Payments → Other fees** and in the student **Create bill** menu.

---

## 5. Students

### List (People → Students)

**Who:** View — most roles. Manage — School manager, Administrator. Create bill — Accountant, Administrator.

1. Search by student number, first name, last name, or **first + last** (either order).
2. Filter by level, group, status; page through results.
3. **Create** — enter identity, contacts, level, group, school year, enrollment date, status. A student number is assigned on save.
4. **Edit** — update the file.
5. **Open** — student detail (fees, payments, receipts, schedule context).
6. **Transfer** — move to another **active** group (level follows the destination group). Optional reason.
7. **Archive** — soft-delete; financial history is kept (confirmation required).
8. **Create bill** (dropdown) — bill this student for Écolage, Droit, Livre, exams, or a custom type.

### Student detail

1. Open from the list or from a group’s student list.
2. Review identity, fee lines, payments, receipts.
3. **Edit** student (if permitted).
4. **Register a payment** — dialog opens with the student (and optionally a selected fee) prefilled.
5. Open a payment for cancel/reverse (Administrator) or receipt details.
6. **Back** returns to the student list.

### Transfer workflow

1. Select student → **Transfer**.
2. Choose destination group (must be active, different from current).
3. Confirm. Academic level updates to match the group.

---

## 6. Attendance

### Daily attendance (Attendance → Daily attendance)

**Who:** School manager, Administrator  

**Prerequisites:** Group with a **class schedule** for the weekday of the chosen date.

1. Select academic level → student group → date.
2. **Load sheet** — students of that group appear.
3. Set status per student: Present / Absent / Late / Excused; optional remarks.
4. Shortcuts: mark all present, or mark selected absent.
5. **Save**.

If no session is scheduled that weekday, save fails with a clear error.

### Attendance reports (Attendance → Attendance reports)

1. Filter by level, group, date range.
2. **Run** to see per-student presence statistics.
3. Read-only (no export on this screen).

---

## 7. Billing (creating fee obligations)

Billing creates **StudentFee** lines (what the student owes). Collecting money is a separate step (section 8).

**Who can bill:** Accountant, Administrator (`ManageFees`).

Shared options on one-time and monthly bill dialogs:

- **Entire group / all groups** or **One student**
- School year (must be **open**)
- Amount and due date / due day
- Optional “only active enrolments” when billing a group

### Droit (Payments → Droit)

1. Click **Bill Droit**.
2. Choose school year, amount (defaults from payment type), due date, target students.
3. Confirm.

**Rule:** At most **one Droit per student per school year**. Existing lines are skipped.

### Écolage (Payments → Écolage)

1. Click **Generate monthly fees**.
2. Choose school year, months within the year, amount per month, due day (1–28).
3. Target: one student or a group / all groups.
4. Confirm.

**Rule:** One Écolage line per student per **month + year**. Already billed months are skipped.

### Livre (Payments → Livre)

1. Click **Bill Livre**.
2. Enter **Book description** (required), school year, amount, due date, target.
3. Confirm.

**Rule:** Many Livre lines per year are allowed. The same description in the same year for the same student is skipped.

### Mock Exam / Official Exam

Same pattern as Livre: description required; many lines per year, distinguished by description.

### Other fees (Payments → Other fees)

For **custom** payment types only (not the five built-ins).

1. Click **Create bill**.
2. Select the payment type (e.g. Excursion, Trip) — amount fills from the type’s default and updates when you change type.
3. Enter description, school year, due date, target.
4. Confirm.

**Rule:** Same description uniqueness as Livre (many lines per year when descriptions differ).

### Create bill from the student list

Same dialogs as above, locked to the selected student. Useful for one-off billing without opening the fee screen.

---

## 8. Collecting payments & receipts

### Register a payment

**Who:** Cashier, Administrator  

**Entry points:** Shell button · fee lists · unpaid balances · payment history · student detail  

**Prerequisites:** At least one outstanding fee; student’s school year is **open**.

1. Select the student (or keep the prefilled one).
2. Outstanding obligations load — pick the fee line to settle.
3. Amount defaults to the **remaining** balance (partial payments allowed; cannot exceed remaining).
4. Set payment date, method (Cash, Bank transfer, Mobile money, Cheque, Other), optional reference and notes.
5. Leave **print receipt** on if you want a print after save.
6. Confirm.

Payment and receipt are saved together. A possible duplicate (same student / date / amount) shows a confirmation before continuing. If printing fails, the payment still remains registered.

### Receipts (Payments → Receipts)

**Who:** View — roles with receipt access. Print — Cashier, Administrator  

1. Filter by date range and search (receipt number, payment number, student number/name).
2. **Print** or **Export PDF**.
3. Open the linked payment if needed.

Each print increases the print count (reprints remain identifiable).

---

## 9. Unpaid balances & payment history

### Unpaid balances (Payments → Unpaid balances)

1. Opens filtered to **outstanding** fees.
2. Filter, search (including full name), export PDF/Excel.
3. **Register payment** on a selected line.
4. **Refresh overdue** recalculates overdue flags for past-due unpaid lines.

### Payment history (Payments → Payment history)

1. Filter by dates, level, group, type, method, status, cashier (when available).
2. Shortcut for **today**.
3. Register payment, open payment detail, export.

Statuses: **Active**, **Cancelled**, **Reversed**. Page totals count **Active** payments only.

### Cancel or reverse a payment

**Who:** Administrator only  

1. Open payment detail (from history, receipts, or student file).
2. Choose **Cancel** or **Reverse**.
3. Confirm and enter a reason.

Only **Active** payments can be cancelled/reversed. Fee balances are restored; the payment remains in history with the new status. The school year must be open.

---

## 10. Financial reports

**Who:** Accountant, School manager, Administrator  

**Screen:** Reports → Financial reports  

| Section | Content |
|---------|---------|
| Daily payments | Cash of a single day, by method |
| Monthly financial | Expected / collected / remaining over a period |
| By class | Breakdown by student group |
| By payment type | Breakdown by fee type |
| Outstanding balances | What is still owed |

1. Set filters (level, group, type; single date for Daily; range otherwise).
2. Results refresh when filters change.
3. **Export PDF** or **Excel**.

---

## 11. Administration

### Users

**Who:** Administrator  

1. Search by username, name (including first + last), or email.
2. **Create** user: name, username, password, role, optional “must change password”.
3. **Edit** profile and role.
4. **Reset password** (can force change at next login).
5. **Deactivate** / **Reactivate** (accounts are not deleted — they may appear on payment history). You cannot deactivate yourself.

### Audit log

**Who:** Administrator  

Filter by action, user, date range, and search text. Read-only trail of logins, billing, payments, cancellations, and configuration changes.

### Dashboard (Overview)

Read-only snapshot: student counts (incl. by level), groups, collections, outstanding/overdue, absences today, recent payments, collected-by-type. Optionally scoped by school year where the UI offers it.

---

## 12. Business rules cheat sheet

| Fee type | How many per student | Description |
|----------|----------------------|-------------|
| **Droit** | One per **school year** | Not used to tell lines apart |
| **Écolage** | One per **month + year** | N/A |
| **Livre** | Many per year | **Required** — unique per year |
| **Mock / Official Exam** | Many per year | **Required** — unique per year |
| **Custom (Other fees)** | Many per year | **Required** — unique per year |

- Closed school year → no new bills; no register / cancel / reverse payments for students on that year.
- Financial records are cancelled or reversed, not hard-deleted.
- Fee list screens share filters, summary totals, PDF/Excel export, **Register payment**, and **Open student**.
- Name search accepts a single name or **First Last** / **Last First** (case-insensitive) on students, fees, payments, receipts, and users.

---

## Quick start checklist (empty school)

1. Sign in as `admin` → change password.  
2. **Settings** — school name and identity.  
3. Confirm **School year** (current).  
4. Confirm **L1 / L2 / L3** (or create levels).  
5. Create **student groups** and **schedules**.  
6. Adjust **payment type** default amounts.  
7. Enrol **students**.  
8. **Bill** Droit / Écolage / other fees as needed.  
9. **Register payments** and print **receipts**.  
10. Use **Reports** and schedule regular **database backups**.
