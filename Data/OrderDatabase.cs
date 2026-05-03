namespace FoundryCommerceDemo.Data;

using FoundryCommerceDemo.Models;

public static class OrderDatabase
{
    private static readonly Dictionary<string, Order> _orders = new()
    {
        // ── Normal high-value order ──
        ["ORD-50001"] = new()
        {
            OrderId = "ORD-50001",
            CustomerId = "CUST-1001",
            CustomerName = "Jane Smith",
            CustomerEmail = "jane.smith@techcorp.com.tr",
            CustomerTier = "Platinum",
            Items =
            [
                new() { Sku = "LAPTOP-PRO-16", ProductName = "ProBook Laptop 16\" i9/64GB", Quantity = 5, Price = 2499.99m, Weight = 2.1m },
                new() { Sku = "DOCK-TB4", ProductName = "Thunderbolt 4 Docking Station", Quantity = 5, Price = 349.99m, Weight = 0.8m },
                new() { Sku = "MON-4K-27", ProductName = "UltraSharp 27\" 4K Monitor", Quantity = 10, Price = 599.99m, Weight = 6.5m },
            ],
            ShippingAddress = "Maslak Mah. Büyükdere Cad. No:255",
            ShippingCity = "Istanbul",
            ShippingCountry = "TR",
            PaymentMethod = "Corporate Invoice",
            PaymentCardLast4 = "N/A",
            PaymentCardCountry = "TR",
            DeviceFingerprint = "FP-STABLE-9A3B",
            IpAddress = "88.255.10.42",
            IpCountry = "TR",
            PlacedAt = DateTime.UtcNow.AddMinutes(-3)
        },

        // ── Suspicious order — geo mismatch + velocity ──
        ["ORD-50002"] = new()
        {
            OrderId = "ORD-50002",
            CustomerId = "CUST-2055",
            CustomerName = "John Smith",
            CustomerEmail = "j.smith8832@tempmail.xyz",
            CustomerTier = "Standard",
            Items =
            [
                new() { Sku = "GPU-RTX5090", ProductName = "GeForce RTX 5090 24GB", Quantity = 4, Price = 1999.99m, Weight = 1.5m },
                new() { Sku = "SSD-4TB", ProductName = "NVMe SSD 4TB", Quantity = 8, Price = 449.99m, Weight = 0.1m },
            ],
            ShippingAddress = "123 Drop Ship Lane, Unit 7",
            ShippingCity = "Lagos",
            ShippingCountry = "NG",
            PaymentMethod = "Credit Card",
            PaymentCardLast4 = "4477",
            PaymentCardCountry = "US",
            DeviceFingerprint = "FP-NEW-X7Z2",
            IpAddress = "185.220.101.45",
            IpCountry = "RO",
            PlacedAt = DateTime.UtcNow.AddMinutes(-1)
        },

        // ── Normal order with stock challenge ──
        ["ORD-50003"] = new()
        {
            OrderId = "ORD-50003",
            CustomerId = "CUST-3421",
            CustomerName = "Maria Gonzalez",
            CustomerEmail = "maria.g@designs.eu",
            CustomerTier = "Gold",
            Items =
            [
                new() { Sku = "LAPTOP-PRO-16", ProductName = "ProBook Laptop 16\" i9/64GB", Quantity = 3, Price = 2499.99m, Weight = 2.1m },
                new() { Sku = "MOUSE-ERG", ProductName = "Ergonomic Wireless Mouse", Quantity = 3, Price = 79.99m, Weight = 0.2m },
                new() { Sku = "KBD-MECH", ProductName = "Mechanical Keyboard RGB", Quantity = 3, Price = 159.99m, Weight = 0.9m },
            ],
            ShippingAddress = "Carrer de Mallorca 401, 3r",
            ShippingCity = "Barcelona",
            ShippingCountry = "ES",
            PaymentMethod = "Credit Card",
            PaymentCardLast4 = "8812",
            PaymentCardCountry = "ES",
            DeviceFingerprint = "FP-STABLE-Q4R7",
            IpAddress = "83.44.196.72",
            IpCountry = "ES",
            PlacedAt = DateTime.UtcNow.AddMinutes(-5)
        }
    };

    // ── Order velocity tracking (simulated) ──
    private static readonly Dictionary<string, int> _recentOrderCounts = new()
    {
        ["CUST-1001"] = 2,   // 2 orders in last 24h — normal for corporate
        ["CUST-2055"] = 7,   // 7 orders in last 24h — suspicious!
        ["CUST-3421"] = 1,   // 1 order in last 24h — normal
    };

    public static Order? GetOrder(string orderId) =>
        _orders.TryGetValue(orderId, out var order) ? order : null;

    public static List<Order> GetAllOrders() => _orders.Values.ToList();

    public static int GetRecentOrderCount(string customerId) =>
        _recentOrderCounts.TryGetValue(customerId, out var count) ? count : 0;

    public static void UpdateStatus(string orderId, string status)
    {
        if (_orders.TryGetValue(orderId, out var order))
            order.Status = status;
    }
}