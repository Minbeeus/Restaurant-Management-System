using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Restaurant.Domain.Entities;

namespace Restaurant.Infrastructure.Persistence.Configurations;

public class ConfigurationConfigurations : 
    IEntityTypeConfiguration<User>,
    IEntityTypeConfiguration<Table>,
    IEntityTypeConfiguration<Order>,
    IEntityTypeConfiguration<Shift>,
    IEntityTypeConfiguration<Material>,
    IEntityTypeConfiguration<SepayWebhookLog>,
    IEntityTypeConfiguration<Customer>,
    IEntityTypeConfiguration<Payment>,
    IEntityTypeConfiguration<VoidLog>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasIndex(u => u.Username).IsUnique();
    }

    public void Configure(EntityTypeBuilder<Table> builder)
    {
        builder.Property(t => t.RowVersion).IsRowVersion();
        builder.HasIndex(t => t.QrToken).IsUnique();
    }

    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.Property(o => o.RowVersion).IsRowVersion();
        builder.HasIndex(o => o.OrderCode).IsUnique();
        
        builder.HasOne(o => o.CreatedByUser)
            .WithMany()
            .HasForeignKey(o => o.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasOne(p => p.Shift)
            .WithMany()
            .HasForeignKey(p => p.ShiftId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<VoidLog> builder)
    {
        builder.HasOne(vl => vl.Order)
            .WithMany()
            .HasForeignKey(vl => vl.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(vl => vl.OrderItem)
            .WithMany()
            .HasForeignKey(vl => vl.OrderItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<Shift> builder)
    {
        builder.Property(s => s.RowVersion).IsRowVersion();
    }

    public void Configure(EntityTypeBuilder<Material> builder)
    {
        builder.Property(m => m.RowVersion).IsRowVersion();
    }

    public void Configure(EntityTypeBuilder<SepayWebhookLog> builder)
    {
        builder.HasIndex(s => s.TransactionId).IsUnique();
    }

    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasIndex(c => c.PhoneNumber).IsUnique();
    }
}

public class UnitConversionConfiguration : IEntityTypeConfiguration<UnitConversion>
{
    public void Configure(EntityTypeBuilder<UnitConversion> builder)
    {
        builder.HasOne(uc => uc.ToUnit)
            .WithMany()
            .HasForeignKey(uc => uc.ToUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RecipeItemConfiguration : IEntityTypeConfiguration<RecipeItem>
{
    public void Configure(EntityTypeBuilder<RecipeItem> builder)
    {
        builder.HasOne(ri => ri.UnitOfMeasure)
            .WithMany()
            .HasForeignKey(ri => ri.UnitOfMeasureId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ModifierRecipeItemConfiguration : IEntityTypeConfiguration<ModifierRecipeItem>
{
    public void Configure(EntityTypeBuilder<ModifierRecipeItem> builder)
    {
        builder.HasOne(mri => mri.UnitOfMeasure)
            .WithMany()
            .HasForeignKey(mri => mri.UnitOfMeasureId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class GoodsReceiptItemConfiguration : IEntityTypeConfiguration<GoodsReceiptItem>
{
    public void Configure(EntityTypeBuilder<GoodsReceiptItem> builder)
    {
        builder.HasOne(gri => gri.UnitOfMeasure)
            .WithMany()
            .HasForeignKey(gri => gri.UnitOfMeasureId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
