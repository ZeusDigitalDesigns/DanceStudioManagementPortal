using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EatonvilleDanceStudio.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToDancer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Dancers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Dancers");
        }
    }
}
