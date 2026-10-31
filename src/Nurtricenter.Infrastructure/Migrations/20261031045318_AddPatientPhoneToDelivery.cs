using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nurtricenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientPhoneToDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PatientPhone",
                table: "Deliveries",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PatientPhone",
                table: "Deliveries");
        }
    }
}
