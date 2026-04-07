using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace C_GS.Migrations
{
    /// <inheritdoc />
    public partial class AddRaccourciToProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Raccourci",
                table: "Products",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Raccourci",
                table: "Products");
        }
    }
}
