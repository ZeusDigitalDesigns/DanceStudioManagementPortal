using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EatonvilleDanceStudio.Migrations
{
    /// <inheritdoc />
    public partial class AddCompetitionBannerFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BrandColorHex",
                table: "Competitions",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFeaturedAnnouncement",
                table: "Competitions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Competitions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationUrl",
                table: "Competitions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BrandColorHex",
                table: "Competitions");

            migrationBuilder.DropColumn(
                name: "IsFeaturedAnnouncement",
                table: "Competitions");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Competitions");

            migrationBuilder.DropColumn(
                name: "RegistrationUrl",
                table: "Competitions");
        }
    }
}
