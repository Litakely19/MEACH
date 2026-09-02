# School Payment Management

A Windows desktop application for managing a school's students, classes, invoices, monthly scholar fees, and payments. Amounts are recorded in Malagasy Ariary (MGA / Ar).

## Architecture

```text
SchoolManagement
├── SchoolManagement.Domain          Entities, enums, repository contracts
├── SchoolManagement.Application     Services, DTOs, FluentValidation
├── SchoolManagement.Infrastructure  EF Core + SQLite, repositories, PDF/Excel
└── SchoolManagement.WPF             MVVM desktop UI
```

Views contain no business logic. Payments, invoices and receipts are written in a database transaction so a failed save never leaves a half-updated balance. Students, classes and payments use soft delete / cancellation rather than physical deletion of financial history.

## Requirements

- Windows 10 or later
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (to build or publish)
- A published self-contained build does **not** require the SDK on the target PC

## Run from source

```bash
dotnet restore MEACH.slnx
dotnet run --project SchoolManagement.WPF
```

The first launch applies EF Core migrations, seeds roles, payment types, a current school year, and the default administrator.

### Default login

| Field    | Value          |
|----------|----------------|
| Username | `admin`        |
| Password | `ChangeMe123!` |

The application **blocks access until that password is changed**.

## Database

The connection string in `SchoolManagement.WPF/appsettings.json` is:

```json
"DefaultConnection": "Data Source=school_management.db"
```

A relative file name is stored under:

`%LocalAppData%\SchoolManagement\school_management.db`

An absolute path in configuration is honoured as-is. Logs go to `%LocalAppData%\SchoolManagement\logs`. Settings include a database backup action.

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
| Accountant     | Invoices, payment history, financial reports |
| Cashier        | Register payments and issue receipts (cannot delete payments or manage users) |
| School manager | Students, classes, reports |

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
