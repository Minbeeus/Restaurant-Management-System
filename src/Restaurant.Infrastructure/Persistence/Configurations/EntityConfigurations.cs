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
    IEntityTypeConfiguration<Refund>,
    IEntityTypeConfiguration<VoidLog>,
    IEntityTypeConfiguration<ApprovalRequest>,
    IEntityTypeConfiguration<CashMovement>,
    IEntityTypeConfiguration<AuditLogEntry>,
    IEntityTypeConfiguration<PaymentReconciliation>,
    IEntityTypeConfiguration<Register>,
    IEntityTypeConfiguration<ComboGroup>,
    IEntityTypeConfiguration<ComboItem>,
    IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<Register> builder)
    {
        builder.HasIndex(r => r.Code).IsUnique();
        builder.HasMany(r => r.Shifts)
            .WithOne(s => s.Register)
            .HasForeignKey(s => s.RegisterId)
            .OnDelete(DeleteBehavior.Restrict);
    }

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
        builder.HasIndex(p => p.IdempotencyKey).IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL");

        builder.HasOne(p => p.Shift)
            .WithMany()
            .HasForeignKey(p => p.ShiftId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.HasOne(r => r.Order)
            .WithMany()
            .HasForeignKey(r => r.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.RefundedByUser)
            .WithMany()
            .HasForeignKey(r => r.RefundedByUserId)
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

    public void Configure(EntityTypeBuilder<ApprovalRequest> builder)
    {
        builder.Property(a => a.RowVersion).IsRowVersion();

        builder.HasOne(a => a.RequestedByUser)
            .WithMany()
            .HasForeignKey(a => a.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.ApprovedByUser)
            .WithMany()
            .HasForeignKey(a => a.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.RejectedByUser)
            .WithMany()
            .HasForeignKey(a => a.RejectedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<CashMovement> builder)
    {
        builder.HasOne(cm => cm.Shift)
            .WithMany()
            .HasForeignKey(cm => cm.ShiftId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cm => cm.CreatedByUser)
            .WithMany()
            .HasForeignKey(cm => cm.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cm => cm.ApprovalRequest)
            .WithMany()
            .HasForeignKey(cm => cm.ApprovalRequestId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.HasOne(al => al.User)
            .WithMany()
            .HasForeignKey(al => al.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<PaymentReconciliation> builder)
    {
        builder.HasOne(pr => pr.Shift)
            .WithMany()
            .HasForeignKey(pr => pr.ShiftId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pr => pr.ReconciledByUser)
            .WithMany()
            .HasForeignKey(pr => pr.ReconciledByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<Shift> builder)
    {
        builder.Property(s => s.RowVersion).IsRowVersion();
    }

    public void Configure(EntityTypeBuilder<Material> builder)
    {
        builder.Property(m => m.RowVersion).IsRowVersion();
        builder.Property(m => m.CurrentStock).HasPrecision(18, 4);
        builder.Property(m => m.MinStockLevel).HasPrecision(18, 4);
    }

    public void Configure(EntityTypeBuilder<SepayWebhookLog> builder)
    {
        builder.HasIndex(s => s.TransactionId).IsUnique();
    }

    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasIndex(c => c.PhoneNumber).IsUnique();
    }

    public void Configure(EntityTypeBuilder<ComboGroup> builder)
    {
        builder.HasOne(cg => cg.Combo)
            .WithMany(m => m.ComboGroups)
            .HasForeignKey(cg => cg.ComboId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<ComboItem> builder)
    {
        builder.HasOne(ci => ci.Combo)
            .WithMany(m => m.ComboItems)
            .HasForeignKey(ci => ci.ComboId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ci => ci.ComboGroup)
            .WithMany(cg => cg.ComboItems)
            .HasForeignKey(ci => ci.ComboGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ci => ci.MenuItem)
            .WithMany()
            .HasForeignKey(ci => ci.MenuItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasOne(oi => oi.ParentItem)
            .WithMany(oi => oi.ChildItems)
            .HasForeignKey(oi => oi.ParentItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class UnitConversionConfiguration : IEntityTypeConfiguration<UnitConversion>
{
    public void Configure(EntityTypeBuilder<UnitConversion> builder)
    {
        builder.Property(uc => uc.ConversionFactor).HasPrecision(18, 4);
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
        builder.Property(ri => ri.Quantity).HasPrecision(18, 4);
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
        builder.Property(mri => mri.Quantity).HasPrecision(18, 4);
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
