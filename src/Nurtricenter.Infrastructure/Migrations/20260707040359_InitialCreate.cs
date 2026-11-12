using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nurtricenter.Infrastructure.Migrations
{
	/// <inheritdoc />
	public partial class InitialCreate : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "Couriers",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "uuid", nullable: false),
					FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
					Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Couriers", x => x.Id);
				});

			migrationBuilder.CreateTable(
				name: "Routes",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "uuid", nullable: false),
					CourierId = table.Column<Guid>(type: "uuid", nullable: false),
					ScheduledDate = table.Column<DateOnly>(type: "date", nullable: false),
					Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Routes", x => x.Id);
				});

			migrationBuilder.CreateTable(
				name: "Deliveries",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "uuid", nullable: false),
					RouteId = table.Column<Guid>(type: "uuid", nullable: false),
					PackageId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
					PatientId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
					PackageLabelData = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
					AddressDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
					AddressCoordinateX = table.Column<double>(type: "double precision", nullable: false),
					AddressCoordinateY = table.Column<double>(type: "double precision", nullable: false),
					Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
					DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
					EvidencePhotoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
					DigitalSignature = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Deliveries", x => x.Id);
					table.ForeignKey(
						name: "FK_Deliveries_Routes_RouteId",
						column: x => x.RouteId,
						principalTable: "Routes",
						principalColumn: "Id",
						onDelete: ReferentialAction.Restrict);
				});

			migrationBuilder.CreateIndex(
				name: "IX_Deliveries_RouteId",
				table: "Deliveries",
				column: "RouteId");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "Couriers");

			migrationBuilder.DropTable(
				name: "Deliveries");

			migrationBuilder.DropTable(
				name: "Routes");
		}
	}
}
