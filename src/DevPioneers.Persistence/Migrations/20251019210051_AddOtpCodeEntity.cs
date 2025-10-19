using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevPioneers.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOtpCodeEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OtpCodes_Code_UserId",
                table: "OtpCodes");

            migrationBuilder.DropIndex(
                name: "IX_OtpCodes_UserId_VerifiedAt_ExpiresAt",
                table: "OtpCodes");

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerificationTokenExpiresAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastFailedLoginUtc",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastLoginIpAddress",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedUntilUtc",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MobileVerificationToken",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MobileVerificationTokenExpiresAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationIpAddress",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "OtpCodes",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Purpose",
                table: "OtpCodes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Login",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Mobile",
                table: "OtpCodes",
                type: "nvarchar(15)",
                maxLength: 15,
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
                type: "nvarchar(320)",
                maxLength: 320,
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

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAtUtc",
                value: new DateTime(2025, 10, 19, 21, 0, 47, 57, DateTimeKind.Utc).AddTicks(2741));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAtUtc",
                value: new DateTime(2025, 10, 19, 21, 0, 47, 57, DateTimeKind.Utc).AddTicks(3009));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAtUtc",
                value: new DateTime(2025, 10, 19, 21, 0, 47, 57, DateTimeKind.Utc).AddTicks(3029));

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAtUtc",
                value: new DateTime(2025, 10, 19, 21, 0, 47, 62, DateTimeKind.Utc).AddTicks(7508));

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAtUtc",
                value: new DateTime(2025, 10, 19, 21, 0, 47, 62, DateTimeKind.Utc).AddTicks(7520));

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAtUtc",
                value: new DateTime(2025, 10, 19, 21, 0, 47, 62, DateTimeKind.Utc).AddTicks(7524));

            migrationBuilder.CreateIndex(
                name: "IX_OtpCodes_Code",
                table: "OtpCodes",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_OtpCodes_Email",
                table: "OtpCodes",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_OtpCodes_Email_Purpose_ExpiresAt",
                table: "OtpCodes",
                columns: new[] { "Email", "Purpose", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OtpCodes_Mobile",
                table: "OtpCodes",
                column: "Mobile");

            migrationBuilder.CreateIndex(
                name: "IX_OtpCodes_Mobile_Purpose_ExpiresAt",
                table: "OtpCodes",
                columns: new[] { "Mobile", "Purpose", "ExpiresAt" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_OtpCodes_EmailOrMobile",
                table: "OtpCodes",
                sql: "([Email] IS NOT NULL) OR ([Mobile] IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OtpCodes_Code",
                table: "OtpCodes");

            migrationBuilder.DropIndex(
                name: "IX_OtpCodes_Email",
                table: "OtpCodes");

            migrationBuilder.DropIndex(
                name: "IX_OtpCodes_Email_Purpose_ExpiresAt",
                table: "OtpCodes");

            migrationBuilder.DropIndex(
                name: "IX_OtpCodes_Mobile",
                table: "OtpCodes");

            migrationBuilder.DropIndex(
                name: "IX_OtpCodes_Mobile_Purpose_ExpiresAt",
                table: "OtpCodes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OtpCodes_EmailOrMobile",
                table: "OtpCodes");

            migrationBuilder.DropColumn(
                name: "EmailVerificationTokenExpiresAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastFailedLoginUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastLoginIpAddress",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LockedUntilUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MobileVerificationToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MobileVerificationTokenExpiresAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RegistrationIpAddress",
                table: "Users");

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
                name: "Purpose",
                table: "OtpCodes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldDefaultValue: "Login");

            migrationBuilder.AlterColumn<string>(
                name: "Mobile",
                table: "OtpCodes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(15)",
                oldMaxLength: 15);

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
                oldType: "nvarchar(320)",
                oldMaxLength: 320,
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
                name: "IX_OtpCodes_UserId_VerifiedAt_ExpiresAt",
                table: "OtpCodes",
                columns: new[] { "UserId", "VerifiedAt", "ExpiresAt" });
        }
    }
}
