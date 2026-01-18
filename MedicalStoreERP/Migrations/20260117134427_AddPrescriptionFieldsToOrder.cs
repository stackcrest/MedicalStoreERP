using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalStoreERP.Migrations
{
    /// <inheritdoc />
    public partial class AddPrescriptionFieldsToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_AspNetUsers_UserId",
                table: "Orders");

            migrationBuilder.AddColumn<string>(
                name: "PrescriptionFileName",
                table: "Orders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrescriptionNotes",
                table: "Orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrescriptionUrl",
                table: "Orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PrescriptionVerified",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PrescriptionVerifiedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrescriptionVerifiedByUserId",
                table: "Orders",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresPrescription",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PrescriptionVerifiedByUserId",
                table: "Orders",
                column: "PrescriptionVerifiedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_AspNetUsers_PrescriptionVerifiedByUserId",
                table: "Orders",
                column: "PrescriptionVerifiedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_AspNetUsers_UserId",
                table: "Orders",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_AspNetUsers_PrescriptionVerifiedByUserId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_AspNetUsers_UserId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_PrescriptionVerifiedByUserId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PrescriptionFileName",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PrescriptionNotes",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PrescriptionUrl",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PrescriptionVerified",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PrescriptionVerifiedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PrescriptionVerifiedByUserId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RequiresPrescription",
                table: "Orders");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_AspNetUsers_UserId",
                table: "Orders",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
