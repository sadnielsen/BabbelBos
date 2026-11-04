using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Conversations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialConversations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "conversation");

            migrationBuilder.CreateTable(
                name: "conversations",
                schema: "conversation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    participant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    gnome_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_conversations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "messages",
                schema: "conversation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    author = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reply_kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    prompt_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    safety_category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    safety_rule_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    safety_severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    safety_source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_messages", x => x.id);
                    table.ForeignKey(
                        name: "fk_messages_conversations_conversation_id",
                        column: x => x.conversation_id,
                        principalSchema: "conversation",
                        principalTable: "conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_conversations_tenant_id_participant_id_started_at",
                schema: "conversation",
                table: "conversations",
                columns: new[] { "tenant_id", "participant_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ix_messages_conversation_id_sent_at",
                schema: "conversation",
                table: "messages",
                columns: new[] { "conversation_id", "sent_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "messages",
                schema: "conversation");

            migrationBuilder.DropTable(
                name: "conversations",
                schema: "conversation");
        }
    }
}
