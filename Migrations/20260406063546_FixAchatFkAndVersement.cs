using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace C_GS.Migrations
{
    /// <inheritdoc />
    public partial class FixAchatFkAndVersement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Achats_Fournisseurs_FournisseurId",
                table: "Achats");

            migrationBuilder.DropIndex(
                name: "IX_Achats_FournisseurId",
                table: "Achats");

            migrationBuilder.DropColumn(
                name: "FournisseurId",
                table: "Achats");

            migrationBuilder.AddColumn<decimal>(
                name: "Versement",
                table: "Achats",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_Achats_IdFournisseur",
                table: "Achats",
                column: "IdFournisseur");

            migrationBuilder.AddForeignKey(
                name: "FK_Achats_Fournisseurs_IdFournisseur",
                table: "Achats",
                column: "IdFournisseur",
                principalTable: "Fournisseurs",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Achats_Fournisseurs_IdFournisseur",
                table: "Achats");

            migrationBuilder.DropIndex(
                name: "IX_Achats_IdFournisseur",
                table: "Achats");

            migrationBuilder.DropColumn(
                name: "Versement",
                table: "Achats");

            migrationBuilder.AddColumn<int>(
                name: "FournisseurId",
                table: "Achats",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Achats_FournisseurId",
                table: "Achats",
                column: "FournisseurId");

            migrationBuilder.AddForeignKey(
                name: "FK_Achats_Fournisseurs_FournisseurId",
                table: "Achats",
                column: "FournisseurId",
                principalTable: "Fournisseurs",
                principalColumn: "Id");
        }
    }
}
