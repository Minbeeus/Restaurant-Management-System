using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Restaurant.Domain.Common;
using Restaurant.Domain.Entities;

namespace Restaurant.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Table> Tables => Set<Table>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<ModifierGroup> ModifierGroups => Set<ModifierGroup>();
    public DbSet<ModifierOption> ModifierOptions => Set<ModifierOption>();
    public DbSet<MenuItemModifierGroup> MenuItemModifierGroups => Set<MenuItemModifierGroup>();

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderItemModifier> OrderItemModifiers => Set<OrderItemModifier>();
    public DbSet<VoidLog> VoidLogs => Set<VoidLog>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<SepayWebhookLog> SepayWebhookLogs => Set<SepayWebhookLog>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<Voucher> Vouchers => Set<Voucher>();

    public DbSet<CustomerTier> CustomerTiers => Set<CustomerTier>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<LoyaltyTransaction> LoyaltyTransactions => Set<LoyaltyTransaction>();

    public DbSet<UnitOfMeasure> UnitOfMeasures => Set<UnitOfMeasure>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<UnitConversion> UnitConversions => Set<UnitConversion>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeItem> RecipeItems => Set<RecipeItem>();
    public DbSet<ModifierRecipe> ModifierRecipes => Set<ModifierRecipe>();
    public DbSet<ModifierRecipeItem> ModifierRecipeItems => Set<ModifierRecipeItem>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<GoodsReceiptItem> GoodsReceiptItems => Set<GoodsReceiptItem>();
    public DbSet<Stocktake> Stocktakes => Set<Stocktake>();
    public DbSet<StocktakeItem> StocktakeItems => Set<StocktakeItem>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Global Query Filter cho Soft Delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(FullAuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var propertyMethodInfo = typeof(EF).GetMethod(nameof(EF.Property))!.MakeGenericMethod(typeof(bool));
                var isDeletedProperty = Expression.Call(propertyMethodInfo, parameter, Expression.Constant("IsDeleted"));
                var compareExpression = Expression.MakeBinary(ExpressionType.Equal, isDeletedProperty, Expression.Constant(false));
                var lambda = Expression.Lambda(compareExpression, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }
}
