using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Infrastructure.Migrations
{
    /// <summary>
    /// Adds UserId column to refresh_tokens so refresh handlers can look up
    /// the token owner to issue user-specific access tokens.
    /// </summary>
    public partial class AddRefreshTokenUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Delete existing refresh tokens BEFORE adding the non-null UserId column.
            // Using Guid.Empty as a default would violate the UserId.From(guid) converter
            // invariant (UserId cannot be empty), breaking EF materialization of existing rows.
            // Existing tokens are ephemeral and safe to discard.
            migrationBuilder.Sql("DELETE FROM refresh_tokens");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "refresh_tokens",
                type: "uuid",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "refresh_tokens");
        }
    }
}
