using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DefaultInstrumentId",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultPageSize",
                table: "Users",
                type: "nvarchar(50)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_DefaultInstrumentId",
                table: "Users",
                column: "DefaultInstrumentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Instruments_DefaultInstrumentId",
                table: "Users",
                column: "DefaultInstrumentId",
                principalTable: "Instruments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Instruments_DefaultInstrumentId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_DefaultInstrumentId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DefaultInstrumentId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DefaultPageSize",
                table: "Users");
        }
    }
}
