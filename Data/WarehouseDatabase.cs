namespace FoundryCommerceDemo.Data;

using FoundryCommerceDemo.Models;

public static class WarehouseDatabase
{
    private static readonly Dictionary<string, WarehouseStock> _warehouses = new()
    {
        ["WH-IST"] = new()
        {
            WarehouseId = "WH-IST",
            WarehouseName = "Istanbul Distribution Center",
            City = "Istanbul",
            Country = "TR",
            Stock = new()
            {
                ["LAPTOP-PRO-16"] = 12,
                ["DOCK-TB4"] = 30,
                ["MON-4K-27"] = 8,   // Not enough for 10!
                ["GPU-RTX5090"] = 2,
                ["SSD-4TB"] = 50,
                ["MOUSE-ERG"] = 100,
                ["KBD-MECH"] = 45,
            }
        },
        ["WH-FRA"] = new()
        {
            WarehouseId = "WH-FRA",
            WarehouseName = "Frankfurt EU Hub",
            City = "Frankfurt",
            Country = "DE",
            Stock = new()
            {
                ["LAPTOP-PRO-16"] = 25,
                ["DOCK-TB4"] = 15,
                ["MON-4K-27"] = 40,
                ["GPU-RTX5090"] = 6,
                ["SSD-4TB"] = 80,
                ["MOUSE-ERG"] = 200,
                ["KBD-MECH"] = 60,
            }
        },
        ["WH-DXB"] = new()
        {
            WarehouseId = "WH-DXB",
            WarehouseName = "Dubai MENA Hub",
            City = "Dubai",
            Country = "AE",
            Stock = new()
            {
                ["LAPTOP-PRO-16"] = 8,
                ["DOCK-TB4"] = 10,
                ["MON-4K-27"] = 15,
                ["GPU-RTX5090"] = 0,
                ["SSD-4TB"] = 20,
                ["MOUSE-ERG"] = 50,
                ["KBD-MECH"] = 25,
            }
        }
    };

    // Shipping cost matrix (simplified: warehouse -> country -> cost per kg)
    private static readonly Dictionary<string, Dictionary<string, decimal>> _shippingRates = new()
    {
        ["WH-IST"] = new() { ["TR"] = 2.5m, ["DE"] = 8.0m, ["ES"] = 9.5m, ["NG"] = 15.0m, ["AE"] = 7.0m },
        ["WH-FRA"] = new() { ["TR"] = 8.0m, ["DE"] = 2.0m, ["ES"] = 4.5m, ["NG"] = 18.0m, ["AE"] = 12.0m },
        ["WH-DXB"] = new() { ["TR"] = 7.0m, ["DE"] = 12.0m, ["ES"] = 14.0m, ["NG"] = 10.0m, ["AE"] = 1.5m },
    };

    // Transit days (warehouse -> country)
    private static readonly Dictionary<string, Dictionary<string, int>> _transitDays = new()
    {
        ["WH-IST"] = new() { ["TR"] = 1, ["DE"] = 3, ["ES"] = 4, ["NG"] = 7, ["AE"] = 3 },
        ["WH-FRA"] = new() { ["TR"] = 3, ["DE"] = 1, ["ES"] = 2, ["NG"] = 8, ["AE"] = 5 },
        ["WH-DXB"] = new() { ["TR"] = 3, ["DE"] = 5, ["ES"] = 6, ["NG"] = 4, ["AE"] = 1 },
    };

    public static List<WarehouseStock> GetAllWarehouses() => _warehouses.Values.ToList();

    public static int GetStock(string warehouseId, string sku) =>
        _warehouses.TryGetValue(warehouseId, out var wh) &&
        wh.Stock.TryGetValue(sku, out var qty) ? qty : 0;

    public static decimal GetShippingRate(string warehouseId, string country) =>
        _shippingRates.TryGetValue(warehouseId, out var rates) &&
        rates.TryGetValue(country, out var rate) ? rate : 20.0m;

    public static int GetTransitDays(string warehouseId, string country) =>
        _transitDays.TryGetValue(warehouseId, out var days) &&
        days.TryGetValue(country, out var d) ? d : 10;

    public static bool ReserveStock(string warehouseId, string sku, int quantity)
    {
        if (!_warehouses.TryGetValue(warehouseId, out var wh)) return false;
        if (!wh.Stock.TryGetValue(sku, out var current) || current < quantity) return false;
        wh.Stock[sku] = current - quantity;
        return true;
    }
}