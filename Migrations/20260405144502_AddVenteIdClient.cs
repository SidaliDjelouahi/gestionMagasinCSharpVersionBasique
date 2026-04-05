using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace C_GS.Migrations
{
    /// <inheritdoc />
    public partial class AddVenteIdClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClientId",
                table: "Ventes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdClient",
                table: "Ventes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ventes_ClientId",
                table: "Ventes",
                column: "ClientId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ventes_Clients_ClientId",
                table: "Ventes",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ventes_Clients_ClientId",
                table: "Ventes");

            migrationBuilder.DropIndex(
                name: "IX_Ventes_ClientId",
                table: "Ventes");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "Ventes");

            migrationBuilder.DropColumn(
                name: "IdClient",
                table: "Ventes");
        }
    }
}
