using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EatonvilleDanceStudio.Migrations
{
    /// <inheritdoc />
    public partial class AddCostumeOrderTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BustChest",
                table: "Dancers",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Girth",
                table: "Dancers",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Hips",
                table: "Dancers",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Inseam",
                table: "Dancers",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LeotardSize",
                table: "Dancers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShoeSize",
                table: "Dancers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TopSize",
                table: "Dancers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Waist",
                table: "Dancers",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CostumeOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DancerId = table.Column<int>(type: "int", nullable: false),
                    DanceClassId = table.Column<int>(type: "int", nullable: false),
                    CostumeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VendorLink = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrderStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpectedDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HandedOutDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostumeOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostumeOrders_DanceClasses_DanceClassId",
                        column: x => x.DanceClassId,
                        principalTable: "DanceClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CostumeOrders_Dancers_DancerId",
                        column: x => x.DancerId,
                        principalTable: "Dancers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CostumeOrders_DanceClassId",
                table: "CostumeOrders",
                column: "DanceClassId");

            migrationBuilder.CreateIndex(
                name: "IX_CostumeOrders_DancerId",
                table: "CostumeOrders",
                column: "DancerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CostumeOrders");

            migrationBuilder.DropColumn(
                name: "BustChest",
                table: "Dancers");

            migrationBuilder.DropColumn(
                name: "Girth",
                table: "Dancers");

            migrationBuilder.DropColumn(
                name: "Hips",
                table: "Dancers");

            migrationBuilder.DropColumn(
                name: "Inseam",
                table: "Dancers");

            migrationBuilder.DropColumn(
                name: "LeotardSize",
                table: "Dancers");

            migrationBuilder.DropColumn(
                name: "ShoeSize",
                table: "Dancers");

            migrationBuilder.DropColumn(
                name: "TopSize",
                table: "Dancers");

            migrationBuilder.DropColumn(
                name: "Waist",
                table: "Dancers");
        }
    }
}
