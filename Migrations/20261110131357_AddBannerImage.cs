using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RacedayAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddBannerImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BannerUrl",
                table: "Events",
                newName: "BannerImage");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BannerImage",
                table: "Events",
                newName: "BannerUrl");
        }
    }
}
