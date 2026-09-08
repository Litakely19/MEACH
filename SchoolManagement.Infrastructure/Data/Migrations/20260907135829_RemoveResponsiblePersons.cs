using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManagement.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveResponsiblePersons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQLite cannot change foreign_keys inside a transaction, and rebuilding
            // Students while Attendances/Payments still reference it fails with
            // FOREIGN KEY constraint failed. Run the rebuild outside a transaction.
            migrationBuilder.Sql(
                """
                PRAGMA foreign_keys = OFF;

                CREATE TABLE "Students_new" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_Students" PRIMARY KEY AUTOINCREMENT,
                    "StudentNumber" TEXT NOT NULL,
                    "FirstName" TEXT NOT NULL,
                    "LastName" TEXT NOT NULL,
                    "Gender" INTEGER NOT NULL,
                    "DateOfBirth" TEXT NOT NULL,
                    "Address" TEXT NULL,
                    "PhoneNumber" TEXT NULL,
                    "Email" TEXT NULL,
                    "AcademicLevelId" INTEGER NOT NULL,
                    "StudentGroupId" INTEGER NOT NULL,
                    "SchoolYearId" INTEGER NULL,
                    "EnrollmentDate" TEXT NOT NULL,
                    "Status" INTEGER NOT NULL,
                    "UpdatedAt" TEXT NULL,
                    "IsDeleted" INTEGER NOT NULL,
                    "DeletedAt" TEXT NULL,
                    "CreatedAt" TEXT NOT NULL,
                    CONSTRAINT "FK_Students_AcademicLevels_AcademicLevelId" FOREIGN KEY ("AcademicLevelId") REFERENCES "AcademicLevels" ("Id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_Students_SchoolYears_SchoolYearId" FOREIGN KEY ("SchoolYearId") REFERENCES "SchoolYears" ("Id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_Students_StudentGroups_StudentGroupId" FOREIGN KEY ("StudentGroupId") REFERENCES "StudentGroups" ("Id") ON DELETE RESTRICT
                );

                INSERT INTO "Students_new" (
                    "Id", "StudentNumber", "FirstName", "LastName", "Gender", "DateOfBirth", "Address", "PhoneNumber", "Email",
                    "AcademicLevelId", "StudentGroupId", "SchoolYearId", "EnrollmentDate", "Status", "UpdatedAt", "IsDeleted", "DeletedAt", "CreatedAt")
                SELECT
                    "Id", "StudentNumber", "FirstName", "LastName", "Gender", "DateOfBirth", "Address", "PhoneNumber", "Email",
                    "AcademicLevelId", "StudentGroupId", "SchoolYearId", "EnrollmentDate", "Status", "UpdatedAt", "IsDeleted", "DeletedAt", "CreatedAt"
                FROM "Students";

                DROP TABLE "Students";
                ALTER TABLE "Students_new" RENAME TO "Students";

                CREATE INDEX "IX_Students_AcademicLevelId_StudentGroupId_Status" ON "Students" ("AcademicLevelId", "StudentGroupId", "Status");
                CREATE INDEX "IX_Students_IsDeleted" ON "Students" ("IsDeleted");
                CREATE INDEX "IX_Students_LastName" ON "Students" ("LastName");
                CREATE INDEX "IX_Students_SchoolYearId" ON "Students" ("SchoolYearId");
                CREATE INDEX "IX_Students_StudentGroupId" ON "Students" ("StudentGroupId");
                CREATE UNIQUE INDEX "IX_Students_StudentNumber" ON "Students" ("StudentNumber");

                DROP TABLE "ResponsiblePersons";

                PRAGMA foreign_keys = ON;
                """,
                suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                PRAGMA foreign_keys = OFF;

                CREATE TABLE "ResponsiblePersons" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_ResponsiblePersons" PRIMARY KEY AUTOINCREMENT,
                    "FirstName" TEXT NOT NULL,
                    "LastName" TEXT NOT NULL,
                    "Relationship" TEXT NOT NULL,
                    "PhoneNumber" TEXT NOT NULL,
                    "Email" TEXT NULL,
                    "Address" TEXT NULL,
                    "UpdatedAt" TEXT NULL,
                    "IsDeleted" INTEGER NOT NULL,
                    "DeletedAt" TEXT NULL,
                    "CreatedAt" TEXT NOT NULL
                );

                CREATE INDEX "IX_ResponsiblePersons_IsDeleted" ON "ResponsiblePersons" ("IsDeleted");
                CREATE INDEX "IX_ResponsiblePersons_LastName" ON "ResponsiblePersons" ("LastName");
                CREATE INDEX "IX_ResponsiblePersons_PhoneNumber" ON "ResponsiblePersons" ("PhoneNumber");

                CREATE TABLE "Students_new" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_Students" PRIMARY KEY AUTOINCREMENT,
                    "StudentNumber" TEXT NOT NULL,
                    "FirstName" TEXT NOT NULL,
                    "LastName" TEXT NOT NULL,
                    "Gender" INTEGER NOT NULL,
                    "DateOfBirth" TEXT NOT NULL,
                    "Address" TEXT NULL,
                    "PhoneNumber" TEXT NULL,
                    "Email" TEXT NULL,
                    "AcademicLevelId" INTEGER NOT NULL,
                    "StudentGroupId" INTEGER NOT NULL,
                    "ResponsiblePersonId" INTEGER NULL,
                    "SchoolYearId" INTEGER NULL,
                    "EnrollmentDate" TEXT NOT NULL,
                    "Status" INTEGER NOT NULL,
                    "UpdatedAt" TEXT NULL,
                    "IsDeleted" INTEGER NOT NULL,
                    "DeletedAt" TEXT NULL,
                    "CreatedAt" TEXT NOT NULL,
                    CONSTRAINT "FK_Students_AcademicLevels_AcademicLevelId" FOREIGN KEY ("AcademicLevelId") REFERENCES "AcademicLevels" ("Id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_Students_ResponsiblePersons_ResponsiblePersonId" FOREIGN KEY ("ResponsiblePersonId") REFERENCES "ResponsiblePersons" ("Id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_Students_SchoolYears_SchoolYearId" FOREIGN KEY ("SchoolYearId") REFERENCES "SchoolYears" ("Id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_Students_StudentGroups_StudentGroupId" FOREIGN KEY ("StudentGroupId") REFERENCES "StudentGroups" ("Id") ON DELETE RESTRICT
                );

                INSERT INTO "Students_new" (
                    "Id", "StudentNumber", "FirstName", "LastName", "Gender", "DateOfBirth", "Address", "PhoneNumber", "Email",
                    "AcademicLevelId", "StudentGroupId", "SchoolYearId", "EnrollmentDate", "Status", "UpdatedAt", "IsDeleted", "DeletedAt", "CreatedAt")
                SELECT
                    "Id", "StudentNumber", "FirstName", "LastName", "Gender", "DateOfBirth", "Address", "PhoneNumber", "Email",
                    "AcademicLevelId", "StudentGroupId", "SchoolYearId", "EnrollmentDate", "Status", "UpdatedAt", "IsDeleted", "DeletedAt", "CreatedAt"
                FROM "Students";

                DROP TABLE "Students";
                ALTER TABLE "Students_new" RENAME TO "Students";

                CREATE INDEX "IX_Students_AcademicLevelId_StudentGroupId_Status" ON "Students" ("AcademicLevelId", "StudentGroupId", "Status");
                CREATE INDEX "IX_Students_IsDeleted" ON "Students" ("IsDeleted");
                CREATE INDEX "IX_Students_LastName" ON "Students" ("LastName");
                CREATE INDEX "IX_Students_ResponsiblePersonId" ON "Students" ("ResponsiblePersonId");
                CREATE INDEX "IX_Students_SchoolYearId" ON "Students" ("SchoolYearId");
                CREATE INDEX "IX_Students_StudentGroupId" ON "Students" ("StudentGroupId");
                CREATE UNIQUE INDEX "IX_Students_StudentNumber" ON "Students" ("StudentNumber");

                PRAGMA foreign_keys = ON;
                """,
                suppressTransaction: true);
        }
    }
}
