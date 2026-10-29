using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nurtricenter.Core.Domain.Delivery;

namespace Nurtricenter.Infrastructure.Data.Configurations;

public sealed class DeliveryConfiguration : IEntityTypeConfiguration<Delivery>
{
    public void Configure(EntityTypeBuilder<Delivery> builder)
    {
        builder.ToTable("Deliveries");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .ValueGeneratedNever();

        builder.Ignore("DomainEvents");

        builder.Property(d => d.SequenceOrder)
            .IsRequired(false);

        builder.Property(d => d.FailureReason)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(d => d.RouteId)
            .IsRequired();

        builder.Property(d => d.Status)
            .HasMaxLength(50)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(d => d.CreatedAt)
            .IsRequired();

        // ── Value Object: ValidatedPackage ──────────────────────────
        builder.OwnsOne(d => d.Package, package =>
        {
            package.Property(p => p.PackageId)
                .HasMaxLength(100)
                .IsRequired()
                .HasColumnName("PackageId");

            package.Property(p => p.PatientId)
                .HasMaxLength(100)
                .IsRequired()
                .HasColumnName("PatientId");

            package.Property(p => p.PatientName)
                .HasMaxLength(200)
                .IsRequired()
                .HasColumnName("PatientName");

            package.Property(p => p.LabelData)
                .HasMaxLength(500)
                .IsRequired()
                .HasColumnName("PackageLabelData");
        });

        // ── Value Object: DeliveryAddress ───────────────────────────
        builder.OwnsOne(d => d.Address, address =>
        {
            address.Property(a => a.Description)
                .HasMaxLength(500)
                .IsRequired()
                .HasColumnName("AddressDescription");

            address.OwnsOne(a => a.PlanarCoordinate, coord =>
            {
                coord.Property(c => c.Latitude)
                    .IsRequired()
                    .HasColumnName("AddressCoordinateLatitude");

                coord.Property(c => c.Longitude)
                    .IsRequired()
                    .HasColumnName("AddressCoordinateLongitude");
            });
        });

        // ── Value Object: DeliveryConfirmation (nullable) ──────────
        builder.OwnsOne(d => d.Confirmation, confirmation =>
        {
            confirmation.Property(c => c.DeliveredAt)
                .IsRequired()
                .HasColumnName("DeliveredAt");

            confirmation.Property(c => c.EvidencePhotoUrl)
                .HasMaxLength(500)
                .IsRequired()
                .HasColumnName("EvidencePhotoUrl");

            confirmation.Property(c => c.DigitalSignature)
                .HasMaxLength(200)
                .IsRequired()
                .HasColumnName("DigitalSignature");
        });
    }
}
