using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManagement.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSchoolYearToStudentFees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SchoolYearId",
                table: "StudentFees",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentFees_SchoolYearId",
                table: "StudentFees",
                column: "SchoolYearId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentFees_StudentId_PaymentTypeId_SchoolYearId",
                table: "StudentFees",
                columns: new[] { "StudentId", "PaymentTypeId", "SchoolYearId" });

            migrationBuilder.AddForeignKey(
                name: "FK_StudentFees_SchoolYears_SchoolYearId",
                table: "StudentFees",
                column: "SchoolYearId",
                principalTable: "SchoolYears",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentFees_SchoolYears_SchoolYearId",
                table: "StudentFees");

            migrationBuilder.DropIndex(
                name: "IX_StudentFees_SchoolYearId",
                table: "StudentFees");

            migrationBuilder.DropIndex(
                name: "IX_StudentFees_StudentId_PaymentTypeId_SchoolYearId",
                table: "StudentFees");

            migrationBuilder.DropColumn(
                name: "SchoolYearId",
                table: "StudentFees");
        }
    }
}
