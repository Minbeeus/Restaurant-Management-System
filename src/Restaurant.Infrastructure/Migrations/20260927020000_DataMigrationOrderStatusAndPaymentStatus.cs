using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Restaurant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DataMigrationOrderStatusAndPaymentStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Backfill PaymentStatus based on Payments / Refunds table
            migrationBuilder.Sql(@"
                UPDATE Orders 
                SET PaymentStatus = 2
                WHERE Status = 3 OR Id IN (SELECT OrderId FROM Payments WHERE Status = 2);

                UPDATE Orders 
                SET PaymentStatus = 4
                WHERE Id IN (
                    SELECT r.OrderId 
                    FROM Refunds r 
                    INNER JOIN Orders o ON o.Id = r.OrderId
                    GROUP BY r.OrderId, o.TotalAmount 
                    HAVING SUM(r.Amount) >= o.TotalAmount
                );

                UPDATE Orders 
                SET PaymentStatus = 3
                WHERE Id IN (
                    SELECT r.OrderId 
                    FROM Refunds r 
                    INNER JOIN Orders o ON o.Id = r.OrderId
                    GROUP BY r.OrderId, o.TotalAmount 
                    HAVING SUM(r.Amount) > 0 AND SUM(r.Amount) < o.TotalAmount
                );

                UPDATE Orders 
                SET PaymentStatus = 0 
                WHERE PaymentStatus IS NULL OR PaymentStatus NOT IN (1, 2, 3, 4, 5);
            ");

            // 2. Migrate Order.Status from Old mapping to New mapping
            migrationBuilder.Sql(@"
                UPDATE Orders SET Status = 4 WHERE Status = 3;
                UPDATE Orders SET Status = 5 WHERE Status = 4;
                UPDATE Orders SET Status = 2 WHERE Status = 1;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE Orders SET Status = 3 WHERE Status = 4;
                UPDATE Orders SET Status = 4 WHERE Status = 5;
            ");
        }
    }
}
