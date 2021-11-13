using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LNDSecureCommunicator.ServiceInterface.Migrations
{
    public partial class FixNullablesAgain : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "FileData",
                table: "DecodedMessages",
                type: "BLOB",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "BLOB");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "FileData",
                table: "DecodedMessages",
                type: "BLOB",
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "BLOB",
                oldNullable: true);
        }
    }
}
