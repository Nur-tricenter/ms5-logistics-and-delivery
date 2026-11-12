using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nurtricenter.Core.Domain.Delivery;
using Nurtricenter.Core.Domain.Route;
using Nurtricenter.Core.Domain.Route.Enums;

namespace Nurtricenter.Infrastructure.Data.Configurations;

public sealed class RouteConfiguration : IEntityTypeConfiguration<Route>
{
	public void Configure(EntityTypeBuilder<Route> builder)
	{
		builder.ToTable("Routes");

		builder.HasKey(r => r.Id);

		builder.Ignore("DomainEvents");

		builder.Property(r => r.CourierId)
			.IsRequired();

		builder.Property(r => r.ScheduledDate)
			.IsRequired();

		builder.Property(r => r.CreatedAt)
			.IsRequired();

		builder.Property(r => r.Status)
			.HasMaxLength(50)
			.HasConversion<string>()
			.IsRequired();

		// ── One-to-Many: Route → Deliveries ────────────────────────
		builder.HasMany(r => r.Deliveries)
			.WithOne()
			.HasForeignKey(nameof(Delivery.RouteId))
			.OnDelete(DeleteBehavior.Restrict);

		builder.Navigation(r => r.Deliveries)
			.UsePropertyAccessMode(PropertyAccessMode.Field);
	}
}
