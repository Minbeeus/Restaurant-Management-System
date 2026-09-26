using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Restaurant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpgradeCashierToolsAndManagerApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RegisterId",
                table: "Shifts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "Payments",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RegisterId",
                table: "CashMovements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "AuditLogEntries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RegisterId",
                table: "AuditLogEntries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShiftId",
                table: "AuditLogEntries",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Registers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Registers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_RegisterId",
                table: "Shifts",
                column: "RegisterId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_IdempotencyKey",
                table: "Payments",
                column: "IdempotencyKey",
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_RegisterId",
                table: "CashMovements",
                column: "RegisterId");

            migrationBuilder.CreateIndex(
                name: "IX_Registers_Code",
                table: "Registers",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CashMovements_Registers_RegisterId",
                table: "CashMovements",
                column: "RegisterId",
                principalTable: "Registers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Shifts_Registers_RegisterId",
                table: "Shifts",
                column: "RegisterId",
                principalTable: "Registers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CashMovements_Registers_RegisterId",
                table: "CashMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_Shifts_Registers_RegisterId",
                table: "Shifts");

            migrationBuilder.DropTable(
                name: "Registers");

            migrationBuilder.DropIndex(
                name: "IX_Shifts_RegisterId",
                table: "Shifts");

            migrationBuilder.DropIndex(
                name: "IX_Payments_IdempotencyKey",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_CashMovements_RegisterId",
                table: "CashMovements");

            migrationBuilder.DropColumn(
                name: "RegisterId",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "RegisterId",
                table: "CashMovements");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "AuditLogEntries");

            migrationBuilder.DropColumn(
                name: "RegisterId",
                table: "AuditLogEntries");

            migrationBuilder.DropColumn(
                name: "ShiftId",
                table: "AuditLogEntries");
        }
    }
}
