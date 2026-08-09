using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeTracker.Server.Migrations
{
    /// <inheritdoc />
    public partial class ChatConversationDiscussedPerfumes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<Guid>>(
                name: "DiscussedPerfumeIds",
                table: "ChatConversation",
                type: "uuid[]",
                nullable: false,
                defaultValue: new List<Guid>());
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscussedPerfumeIds",
                table: "ChatConversation");
        }
    }
}
