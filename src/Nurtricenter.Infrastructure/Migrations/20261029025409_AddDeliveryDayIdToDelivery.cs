using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nurtricenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryDayIdToDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeliveryDayId",
                table: "Deliveries",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryDayId",
                table: "Deliveries");
        }
    }
}
