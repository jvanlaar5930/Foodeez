using Foodeez.Domain.Entities;
using Foodeez.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Foodeez.Infrastructure.Data.Configurations;

public class ChatConversationConfiguration : IEntityTypeConfiguration<ChatConversation>
{
    public void Configure(EntityTypeBuilder<ChatConversation> builder)
    {
        builder.ToTable("chat_conversations");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Title).HasMaxLength(200).IsRequired();

        // The thread list is always "this user's, newest first".
        builder.HasIndex(c => new { c.UserId, c.LastMessageAt });

        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Messages)
            .WithOne(m => m.Conversation)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("chat_messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Role).HasConversion<int>();

        // A model's answer runs long, and truncating advice mid-sentence is worse than the
        // storage it saves.
        builder.Property(m => m.Content).HasColumnType("text").IsRequired();

        builder.Property(m => m.Suggestions)
            .HasColumnType("text")
            .HasConversion(JsonValueList<PlannedMeal>.Converter, JsonValueList<PlannedMeal>.Comparer);

        builder.Property(m => m.Recipes)
            .HasColumnType("text")
            .HasConversion(JsonValueList<SuggestedRecipe>.Converter, JsonValueList<SuggestedRecipe>.Comparer);

        // Messages are only ever read as a whole thread in order.
        builder.HasIndex(m => new { m.ConversationId, m.CreatedAt });
    }
}
