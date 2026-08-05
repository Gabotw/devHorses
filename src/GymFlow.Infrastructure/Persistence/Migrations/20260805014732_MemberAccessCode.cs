using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MemberAccessCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccessCode",
                table: "members",
                type: "character(4)",
                fixedLength: true,
                maxLength: 4,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_members_TenantId_AccessCode",
                table: "members",
                columns: new[] { "TenantId", "AccessCode" },
                unique: true,
                filter: "\"AccessCode\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_members_TenantId_AccessCode",
                table: "members");

            migrationBuilder.DropColumn(
                name: "AccessCode",
                table: "members");
        }
    }
}
