using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ServiceUsers.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddManagerAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "manager_assignments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    manager_email = table.Column<string>(type: "text", nullable: false),
                    partner_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_manager_assignments", x => x.id);
                    table.ForeignKey(
                        name: "fk_manager_assignments_partners_partner_id",
                        column: x => x.partner_id,
                        principalTable: "partners",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_manager_assignments_users_manager_email",
                        column: x => x.manager_email,
                        principalTable: "users",
                        principalColumn: "email",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_manager_assignments_manager_email_partner_id",
                table: "manager_assignments",
                columns: new[] { "manager_email", "partner_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_manager_assignments_partner_id",
                table: "manager_assignments",
                column: "partner_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "manager_assignments");
        }
    }
}
