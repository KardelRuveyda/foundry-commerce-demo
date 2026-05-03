namespace FoundryCommerceDemo.Models;

public class Order
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerTier { get; set; } = "Standard"; // Standard, Gold, Platinum
    public List<LineItem> Items { get; set; } = [];
    public string ShippingAddress { get; set; } = string.Empty;
    public string ShippingCity { get; set; } = string.Empty;
    public string ShippingCountry { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentCardLast4 { get; set; } = string.Empty;
    public string PaymentCardCountry { get; set; } = string.Empty;
    public string DeviceFingerprint { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string IpCountry { get; set; } = string.Empty;
    public decimal TotalAmount => Items.Sum(i => i.Price * i.Quantity);
    public DateTime PlacedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending";
}

public class LineItem
{
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Weight { get; set; } // kg
}

public class WarehouseStock
{
    public string WarehouseId { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public Dictionary<string, int> Stock { get; set; } = new(); // SKU -> quantity
}