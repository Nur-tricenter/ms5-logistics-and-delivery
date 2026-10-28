namespace Nurtricenter.Infrastructure.Messaging;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// One row per integration message already handled. Its primary key IS the message id, so a
/// redelivery is detected before the handler runs, and a concurrent duplicate collides on insert
/// (see <see cref="IdempotentConsumer{T,TInner}"/>).
/// </summary>
public sealed class InboxMessage
{
    private InboxMessage() { } // EF Core

    public InboxMessage(Guid messageId, string messageType, string? correlationId, DateTimeOffset createdAt, DateTimeOffset processedOn)
    {
        MessageId = messageId;
        MessageType = messageType;
        CorrelationId = correlationId;
        // Npgsql only writes UTC DateTimeOffsets to timestamptz.
        MessageCreatedAt = createdAt.ToUniversalTime();
        ProcessedOn = processedOn.ToUniversalTime();
    }

    public Guid MessageId { get; private set; }

    public string MessageType { get; private set; } = string.Empty;

    public string? CorrelationId { get; private set; }

    public DateTimeOffset MessageCreatedAt { get; private set; }

    public DateTimeOffset ProcessedOn { get; private set; }
}

public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("InboxMessages");

        builder.HasKey(m => m.MessageId);

        builder.Property(m => m.MessageId)
            .ValueGeneratedNever();

        builder.Property(m => m.MessageType)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(m => m.CorrelationId)
            .HasMaxLength(128);

        builder.HasIndex(m => m.ProcessedOn);
    }
}
