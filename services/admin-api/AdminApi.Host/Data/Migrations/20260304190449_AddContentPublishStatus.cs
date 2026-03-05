using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminApi.Host.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddContentPublishStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "Contents",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "Contents");
        }
    }
}
