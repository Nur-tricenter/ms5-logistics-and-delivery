using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nurtricenter.Core.Domain.Courier;
using Nurtricenter.Core.Domain.Courier.Enums;

namespace Nurtricenter.Infrastructure.Data.Configurations;

public sealed class CourierConfiguration : IEntityTypeConfiguration<Courier>
{
	public void Configure(EntityTypeBuilder<Courier> builder)
	{
		builder.ToTable("Couriers");

		builder.HasKey(c => c.Id);

		builder.Ignore("DomainEvents");

		builder.Property(c => c.FullName)
			.HasMaxLength(200)
			.IsRequired();

		builder.Property(c => c.Status)
			.HasMaxLength(50)
			.HasConversion<string>()
			.IsRequired();
	}
}
