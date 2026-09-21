# School / Training Center Management

A Windows desktop application for managing a school or training center: students, academic levels (L1, L2, L3), independent student groups, class schedules, attendance, individual payment obligations, receipts, and financial reports. Amounts are recorded in Malagasy Ariary (MGA / Ar).

The application runs entirely on the local PC. It does not require a separate database server.

## Architecture

```text
SchoolManagement
├── SchoolManagement.Domain          Entities, enums, repository contracts
├── SchoolManagement.Application     Services, DTOs, validators
├── SchoolManagement.Infrastructure  EF Core + SQLite, repositories, PDF/Excel
└── SchoolManagement.WPF             MVVM desktop UI
```

Views contain no business logic. Payments and receipts are written in a database transaction so a failed save never leaves a half-updated balance. Financial records are cancelled or reversed rather than permanently deleted.

Academic structure:

```text
Academic Level (L1, L2, L3)
 └── Student Group (e.g. L1 Group 09:00 - 10:00)
      ├── Students (each student belongs to one level and one group)
      └── Class schedules (day + start/end time)
           └── Attendance (per group, never mixed across groups)
```

Money is tracked per student through a unified `StudentFee` obligation (monthly Écolage with month/year, or one-time Droit / Livre / Mock Exam / Official Exam). Several payments can settle the same obligation.

## Requirements

- Windows 10 or later
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (to build or publish)
- A published self-contained build does **not** require the SDK on the target PC

## Run from source

```bash
dotnet restore MEACH.slnx
dotnet run --project SchoolManagement.WPF
```

The first launch applies EF Core migrations and seeds bootstrap data (roles, administrator, levels, payment types, settings, school year). When `Application:SeedDemoData` is `true` (default in `appsettings.json`), it also loads sample groups, students, fees, payments, and demo users.

To reload fake data on an existing install, delete `%LocalAppData%\SchoolManagement\school_management.db` and start the app again.

### Default login

| Field    | Value          |
|----------|----------------|
| Username | `admin`        |
| Password | `ChangeMe123!` |

The application **blocks access until that password is changed**.

## User documentation

Full end-user workflows: [docs/WORKFLOWS.md](docs/WORKFLOWS.md) · [docs/WORKFLOWS.pdf](docs/WORKFLOWS.pdf).  
Scenario image workflows (flowcharts): [docs/SCENARIO_FLOWS.pdf](docs/SCENARIO_FLOWS.pdf).

## Database

The connection string in `SchoolManagement.WPF/appsettings.json` is:

```json
"DefaultConnection": "Data Source=school_management.db"
```

A relative file name is stored under:

`%LocalAppData%\SchoolManagement\school_management.db`

An absolute path in configuration is honoured as-is. Logs go to `%LocalAppData%\SchoolManagement\logs`. Settings include a database backup action.

If you already used an earlier version of this application, **delete the old database file** before the first launch of this schema. The previous invoice/class model is not upgraded in place.

New schema changes:

```bash
dotnet ef migrations add <Name> \
  --project SchoolManagement.Infrastructure \
  --startup-project SchoolManagement.WPF \
  --output-dir Data/Migrations
```

## Roles

| Role           | Access |
|----------------|--------|
| Administrator  | Full access, including users and settings |
| Accountant     | Payments, financial information and reports |
| Cashier        | Register payments and issue receipts (cannot cancel/reverse payments or manage users) |
| School manager | Students, academic levels, groups, schedules, attendance and reports |

## Modules

- Dashboard — student counts by L1/L2/L3, collections, overdue fees, absences, recent payments
- Students — list, file (schedule, attendance, fees, payments, receipts), transfer between groups
- Academic levels — L1 / L2 / L3, groups under each level
- Student groups — independent groups per level, with class sessions
- Attendance — daily sheet per group and session, plus attendance reports
- Payments — Droit, Écolage, Livre, Mock Exam, Official Exam, unpaid balances, history, receipts
- Financial reports — daily cash, monthly income, by group, by type, outstanding balances (PDF and Excel)
- Administration — users, payment types, audit log, settings

## Publish a Windows executable

Self-contained (includes the .NET runtime, can run on a PC without .NET installed):

```bash
dotnet publish SchoolManagement.WPF/SchoolManagement.WPF.csproj -c Release -r win-x64 --self-contained true -o ./publish
```

Single-file executable (one `.exe`, plus the SQLite native library beside it):

```bash
dotnet publish SchoolManagement.WPF/SchoolManagement.WPF.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish-single
```

Copy the contents of the output folder to the school PC and run `SchoolManagement.WPF.exe`. Keep `appsettings.json` next to the executable (the single-file publish copies it automatically).

The SQLite database is **not** stored next to the executable. It lives in the per-user application data folder so upgrades do not overwrite financial data.
