using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace C_GS.Migrations
{
    /// <inheritdoc />
    public partial class AddAchats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Achats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NumAchat = table.Column<string>(type: "TEXT", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IdFournisseur = table.Column<int>(type: "INTEGER", nullable: true),
                    FournisseurId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Achats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Achats_Fournisseurs_FournisseurId",
                        column: x => x.FournisseurId,
                        principalTable: "Fournisseurs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AchatDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IdAchat = table.Column<int>(type: "INTEGER", nullable: false),
                    IdProduit = table.Column<int>(type: "INTEGER", nullable: false),
                    PrixAchat = table.Column<decimal>(type: "TEXT", nullable: false),
                    Qte = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchatDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AchatDetails_Achats_IdAchat",
                        column: x => x.IdAchat,
                        principalTable: "Achats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AchatDetails_IdAchat",
                table: "AchatDetails",
                column: "IdAchat");

            migrationBuilder.CreateIndex(
                name: "IX_Achats_FournisseurId",
                table: "Achats",
                column: "FournisseurId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AchatDetails");

            migrationBuilder.DropTable(
                name: "Achats");
        }
    }
}
