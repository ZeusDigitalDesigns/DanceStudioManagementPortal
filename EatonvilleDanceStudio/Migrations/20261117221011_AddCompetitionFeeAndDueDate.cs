using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EatonvilleDanceStudio.Migrations
{
    /// <inheritdoc />
    public partial class AddCompetitionFeeAndDueDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FeeDueDate",
                table: "Competitions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegistrationDeadline",
                table: "Competitions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RegistrationFee",
                table: "Competitions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FeeDueDate",
                table: "Competitions");

            migrationBuilder.DropColumn(
                name: "RegistrationDeadline",
                table: "Competitions");

            migrationBuilder.DropColumn(
                name: "RegistrationFee",
                table: "Competitions");
        }
    }
}
