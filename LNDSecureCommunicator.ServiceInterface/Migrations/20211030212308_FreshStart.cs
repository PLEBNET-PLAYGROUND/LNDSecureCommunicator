using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LNDSecureCommunicator.ServiceInterface.Migrations
{
    public partial class FreshStart : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DecodedMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NodePubkey = table.Column<string>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    FileData = table.Column<byte[]>(type: "BLOB", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecodedMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LNDSecureCommunicatorSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NodePubkey = table.Column<string>(type: "TEXT", nullable: true),
                    OnionPublicAddress = table.Column<string>(type: "TEXT", nullable: false),
                    KeyType = table.Column<string>(type: "TEXT", nullable: false),
                    OnionPrivateKeyBase32 = table.Column<string>(type: "TEXT", nullable: false),
                    ClientAuthBase64PrivateKey = table.Column<string>(type: "TEXT", nullable: false),
                    ClientAuthBase32PublicKey = table.Column<string>(type: "TEXT", nullable: false),
                    InvoiceLastIndexOffset = table.Column<ulong>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LNDSecureCommunicatorSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RemoteNodes",
                columns: table => new
                {
                    NodePubkey = table.Column<string>(type: "TEXT", nullable: false),
                    RemoteNodeACK = table.Column<bool>(type: "INTEGER", nullable: false),
                    SharedBase64PrivateKey = table.Column<string>(type: "TEXT", nullable: false),
                    OnionAddress = table.Column<string>(type: "TEXT", nullable: false),
                    ClientAuthBase32PublicKey = table.Column<string>(type: "TEXT", nullable: false),
                    Disabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemoteNodes", x => x.NodePubkey);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DecodedMessages_CreatedDate",
                table: "DecodedMessages",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_DecodedMessages_NodePubkey",
                table: "DecodedMessages",
                column: "NodePubkey");

            migrationBuilder.CreateIndex(
                name: "IX_RemoteNodes_Disabled",
                table: "RemoteNodes",
                column: "Disabled");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DecodedMessages");

            migrationBuilder.DropTable(
                name: "LNDSecureCommunicatorSettings");

            migrationBuilder.DropTable(
                name: "RemoteNodes");
        }
    }
}
