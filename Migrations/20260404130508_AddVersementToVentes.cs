using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace C_GS.Migrations
{
    /// <inheritdoc />
    public partial class AddVersementToVentes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Only add the new column `Versement` to existing Ventes table
            migrationBuilder.AddColumn<decimal>(
                name: "Versement",
                table: "Ventes",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Versement",
                table: "Ventes");
        }
    }
}
