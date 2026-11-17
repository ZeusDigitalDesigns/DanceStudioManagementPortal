using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EatonvilleDanceStudio.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "OutstandingBalance",
                table: "Dancers",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "FundraiserEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    EventDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GrossRevenue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Expenses = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetProfit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalDistributed = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SplitMode = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByDirectorId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundraiserEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FundraiserLedgerTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FundraiserEventId = table.Column<int>(type: "int", nullable: false),
                    DancerId = table.Column<int>(type: "int", nullable: false),
                    CreditAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PreviousBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NewBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecordedByDirectorId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundraiserLedgerTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FundraiserLedgerTransactions_Dancers_DancerId",
                        column: x => x.DancerId,
                        principalTable: "Dancers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FundraiserLedgerTransactions_FundraiserEvents_FundraiserEventId",
                        column: x => x.FundraiserEventId,
                        principalTable: "FundraiserEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FundraiserLedgerTransactions_DancerId",
                table: "FundraiserLedgerTransactions",
                column: "DancerId");

            migrationBuilder.CreateIndex(
                name: "IX_FundraiserLedgerTransactions_FundraiserEventId",
                table: "FundraiserLedgerTransactions",
                column: "FundraiserEventId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FundraiserLedgerTransactions");

            migrationBuilder.DropTable(
                name: "FundraiserEvents");

            migrationBuilder.DropColumn(
                name: "OutstandingBalance",
                table: "Dancers");
        }
    }
}
