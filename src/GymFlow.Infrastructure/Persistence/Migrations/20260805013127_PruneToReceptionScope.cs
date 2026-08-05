using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PruneToReceptionScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "class_reservations");

            migrationBuilder.DropTable(
                name: "platform_admins");

            migrationBuilder.DropTable(
                name: "subscriptions");

            migrationBuilder.DropTable(
                name: "class_sessions");

            migrationBuilder.DropTable(
                name: "platform_plans");

            migrationBuilder.DropColumn(
                name: "LastLoginAtUtc",
                table: "members");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "members");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAtUtc",
                table: "members",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "members",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "class_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    InstructorName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    LocalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    StartsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_class_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_class_sessions_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "platform_admins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FullName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastLoginAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PasswordHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_admins", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "platform_plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BillingPeriodDays = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MaxMembers = table.Column<int>(type: "integer", nullable: true),
                    MonthlyPrice = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_plans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "class_reservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttendedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CanceledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClassSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_class_reservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_class_reservations_class_sessions_ClassSessionId",
                        column: x => x.ClassSessionId,
                        principalTable: "class_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_class_reservations_members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BillingPeriodDays = table.Column<int>(type: "integer", nullable: false),
                    CanceledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentPeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    CurrentPeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PlanName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    PlatformPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    PriceAtSubscription = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_subscriptions_platform_plans_PlatformPlanId",
                        column: x => x.PlatformPlanId,
                        principalTable: "platform_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_subscriptions_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_class_reservations_ClassSessionId_MemberId",
                table: "class_reservations",
                columns: new[] { "ClassSessionId", "MemberId" },
                unique: true,
                filter: "\"Status\" IN (1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_class_reservations_MemberId",
                table: "class_reservations",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_class_sessions_TenantId_LocalDate",
                table: "class_sessions",
                columns: new[] { "TenantId", "LocalDate" });

            migrationBuilder.CreateIndex(
                name: "IX_platform_admins_Email",
                table: "platform_admins",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_platform_plans_Name",
                table: "platform_plans",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_PlatformPlanId",
                table: "subscriptions",
                column: "PlatformPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_TenantId",
                table: "subscriptions",
                column: "TenantId",
                unique: true);
        }
    }
}
