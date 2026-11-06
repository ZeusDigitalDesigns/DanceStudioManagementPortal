using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EatonvilleDanceStudio.Migrations
{
    /// <inheritdoc />
    public partial class AddOccupationalDiscountFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMilitaryOrFirstResponder",
                table: "ParentProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsMilitaryOrFirstResponder",
                table: "ParentProfiles");
        }
    }
}
