using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevPioneers.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class updateDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OtpCodes_Mobile_Purpose_Expires",
                table: "OtpCodes");

            migrationBuilder.DropIndex(
                name: "IX_OtpCodes_UserId",
                table: "OtpCodes");

            migrationBuilder.DropIndex(
                name: "IX_AuditTrails_Entity",
                table: "AuditTrails");

            migrationBuilder.DropIndex(
                name: "IX_AuditTrails_Timestamp_Entity",
                table: "AuditTrails");

            migrationBuilder.DropIndex(
                name: "IX_AuditTrails_UserId",
                table: "AuditTrails");

            migrationBuilder.DropColumn(
                name: "ResponseStatusCode",
                table: "AuditTrails");

            migrationBuilder.RenameColumn(
                name: "TimestampUtc",
                table: "AuditTrails",
                newName: "Timestamp");

            migrationBuilder.RenameColumn(
                name: "RequestPath",
                table: "AuditTrails",
                newName: "RequestUrl");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "OtpCodes",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Mobile",
                table: "OtpCodes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<int>(
                name: "MaxAttempts",
                table: "OtpCodes",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 3);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "OtpCodes",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "OtpCodes",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "UserFullName",
                table: "AuditTrails",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Action",
                table: "AuditTrails",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAtUtc",
                value: new DateTime(2025, 10, 17, 22, 48, 58, 757, DateTimeKind.Utc).AddTicks(5602));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAtUtc",
                value: new DateTime(2025, 10, 17, 22, 48, 58, 757, DateTimeKind.Utc).AddTicks(5885));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAtUtc",
                value: new DateTime(2025, 10, 17, 22, 48, 58, 757, DateTimeKind.Utc).AddTicks(5887));

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAtUtc",
                value: new DateTime(2025, 10, 17, 22, 48, 58, 765, DateTimeKind.Utc).AddTicks(3490));

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAtUtc",
                value: new DateTime(2025, 10, 17, 22, 48, 58, 765, DateTimeKind.Utc).AddTicks(3504));

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAtUtc",
                value: new DateTime(2025, 10, 17, 22, 48, 58, 765, DateTimeKind.Utc).AddTicks(3509));

            migrationBuilder.CreateIndex(
                name: "IX_OtpCodes_Code_UserId",
                table: "OtpCodes",
                columns: new[] { "Code", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_OtpCodes_UserId",
                table: "OtpCodes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OtpCodes_UserId_VerifiedAt_ExpiresAt",
                table: "OtpCodes",
                columns: new[] { "UserId", "VerifiedAt", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrails_EntityName",
                table: "AuditTrails",
                column: "EntityName");

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrails_EntityName_EntityId",
                table: "AuditTrails",
                columns: new[] { "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrails_UserId",
                table: "AuditTrails",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrails_UserId_Timestamp",
                table: "AuditTrails",
                columns: new[] { "UserId", "Timestamp" });

            migrationBuilder.AddForeignKey(
                name: "FK_AuditTrails_Users_UserId",
                table: "AuditTrails",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_OtpCodes_Users_UserId",
                table: "OtpCodes",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditTrails_Users_UserId",
                table: "AuditTrails");

            migrationBuilder.DropForeignKey(
                name: "FK_OtpCodes_Users_UserId",
                table: "OtpCodes");

            migrationBuilder.DropIndex(
                name: "IX_OtpCodes_Code_UserId",
                table: "OtpCodes");

            migrationBuilder.DropIndex(
                name: "IX_OtpCodes_UserId",
                table: "OtpCodes");

            migrationBuilder.DropIndex(
                name: "IX_OtpCodes_UserId_VerifiedAt_ExpiresAt",
                table: "OtpCodes");

            migrationBuilder.DropIndex(
                name: "IX_AuditTrails_EntityName",
                table: "AuditTrails");

            migrationBuilder.DropIndex(
                name: "IX_AuditTrails_EntityName_EntityId",
                table: "AuditTrails");

            migrationBuilder.DropIndex(
                name: "IX_AuditTrails_UserId",
                table: "AuditTrails");

            migrationBuilder.DropIndex(
                name: "IX_AuditTrails_UserId_Timestamp",
                table: "AuditTrails");

            migrationBuilder.RenameColumn(
                name: "Timestamp",
                table: "AuditTrails",
                newName: "TimestampUtc");

            migrationBuilder.RenameColumn(
                name: "RequestUrl",
                table: "AuditTrails",
                newName: "RequestPath");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "OtpCodes",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Mobile",
                table: "OtpCodes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "MaxAttempts",
                table: "OtpCodes",
                type: "int",
                nullable: false,
                defaultValue: 3,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "OtpCodes",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "OtpCodes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "UserFullName",
                table: "AuditTrails",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "AuditTrails",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "ResponseStatusCode",
                table: "AuditTrails",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAtUtc",
                value: new DateTime(2025, 10, 16, 13, 37, 53, 15, DateTimeKind.Utc).AddTicks(1816));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAtUtc",
                value: new DateTime(2025, 10, 16, 13, 37, 53, 15, DateTimeKind.Utc).AddTicks(2085));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAtUtc",
                value: new DateTime(2025, 10, 16, 13, 37, 53, 15, DateTimeKind.Utc).AddTicks(2087));

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAtUtc",
                value: new DateTime(2025, 10, 16, 13, 37, 53, 21, DateTimeKind.Utc).AddTicks(387));

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAtUtc",
                value: new DateTime(2025, 10, 16, 13, 37, 53, 21, DateTimeKind.Utc).AddTicks(397));

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAtUtc",
                value: new DateTime(2025, 10, 16, 13, 37, 53, 21, DateTimeKind.Utc).AddTicks(400));

            migrationBuilder.CreateIndex(
                name: "IX_OtpCodes_Mobile_Purpose_Expires",
                table: "OtpCodes",
                columns: new[] { "Mobile", "Purpose", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OtpCodes_UserId",
                table: "OtpCodes",
                column: "UserId",
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrails_Entity",
                table: "AuditTrails",
                columns: new[] { "EntityName", "EntityId" },
                filter: "[EntityId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrails_Timestamp_Entity",
                table: "AuditTrails",
                columns: new[] { "TimestampUtc", "EntityName" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrails_UserId",
                table: "AuditTrails",
                column: "UserId",
                filter: "[UserId] IS NOT NULL");
        }
    }
}
