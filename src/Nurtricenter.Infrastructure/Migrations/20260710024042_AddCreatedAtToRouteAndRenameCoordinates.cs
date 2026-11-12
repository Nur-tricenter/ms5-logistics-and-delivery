using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nurtricenter.Infrastructure.Migrations
{
	/// <inheritdoc />
	public partial class AddCreatedAtToRouteAndRenameCoordinates : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.RenameColumn(
				name: "AddressCoordinateY",
				table: "Deliveries",
				newName: "AddressCoordinateLongitude");

			migrationBuilder.RenameColumn(
				name: "AddressCoordinateX",
				table: "Deliveries",
				newName: "AddressCoordinateLatitude");

			migrationBuilder.AddColumn<DateTime>(
				name: "CreatedAt",
				table: "Routes",
				type: "timestamp with time zone",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "CreatedAt",
				table: "Routes");

			migrationBuilder.RenameColumn(
				name: "AddressCoordinateLongitude",
				table: "Deliveries",
				newName: "AddressCoordinateY");

			migrationBuilder.RenameColumn(
				name: "AddressCoordinateLatitude",
				table: "Deliveries",
				newName: "AddressCoordinateX");
		}
	}
}
