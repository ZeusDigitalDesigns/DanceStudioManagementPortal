using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EatonvilleDanceStudio.Migrations
{
    /// <inheritdoc />
    public partial class AddCompetitionItineraries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Competitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    EventFormat = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VenueName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    VenueAddress = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsStudioAttending = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Competitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompetitionEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompetitionId = table.Column<int>(type: "int", nullable: false),
                    DanceClassId = table.Column<int>(type: "int", nullable: false),
                    RoutineName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    PerformanceOrder = table.Column<int>(type: "int", nullable: false),
                    BackstageCallTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StageTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BackstageNotes = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetitionEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompetitionEntries_Competitions_CompetitionId",
                        column: x => x.CompetitionId,
                        principalTable: "Competitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompetitionEntries_DanceClasses_DanceClassId",
                        column: x => x.DanceClassId,
                        principalTable: "DanceClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionEntries_CompetitionId",
                table: "CompetitionEntries",
                column: "CompetitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionEntries_DanceClassId",
                table: "CompetitionEntries",
                column: "DanceClassId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompetitionEntries");

            migrationBuilder.DropTable(
                name: "Competitions");
        }
    }
}
