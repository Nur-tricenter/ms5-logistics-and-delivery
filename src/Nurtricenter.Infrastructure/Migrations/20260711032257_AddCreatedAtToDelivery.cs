using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nurtricenter.Infrastructure.Migrations
{
	/// <inheritdoc />
	public partial class AddCreatedAtToDelivery : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<DateTime>(
				name: "CreatedAt",
				table: "Deliveries",
				type: "timestamp with time zone",
				nullable: false,
				defaultValue: DateTime.UtcNow);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "CreatedAt",
				table: "Deliveries");
		}
	}
}
