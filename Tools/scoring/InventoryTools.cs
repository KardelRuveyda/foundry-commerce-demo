using System.ComponentModel;
using FoundryCommerceDemo.Data;

namespace FoundryCommerceDemo.Tools;

public static class InventoryTools
{
    [Description("Checks stock availability for all items in an order across all warehouses. " +
                 "Returns per-item, per-warehouse availability matrix showing where items can ship from.")]
    public static string CheckStockAvailability(
        [Description("The order ID to check inventory for")] string orderId)
    {
        var order = OrderDatabase.GetOrder(orderId);
        if (order is null) return $"ERROR: Order {orderId} not found.";

        var warehouses = WarehouseDatabase.GetAllWarehouses();
        var result = $"INVENTORY AVAILABILITY MATRIX -- {orderId}\n";
        result += new string('=', 70) + "\n";
        result += $"{"SKU",-18} {"Need",5}";
        foreach (var wh in warehouses)
            result += $" | {wh.WarehouseId,7}";
        result += " | Status\n";
        result += new string('-', 70) + "\n";

        bool allFromOneWarehouse = true;
        foreach (var item in order.Items)
        {
            result += $"{item.Sku,-18} {item.Quantity,5}";
            bool itemFulfillable = false;
            int totalAcrossWarehouses = 0;

            foreach (var wh in warehouses)
            {
                int stock = WarehouseDatabase.GetStock(wh.WarehouseId, item.Sku);
                totalAcrossWarehouses += stock;
                string display = stock >= item.Quantity ? $"OK {stock}" : stock > 0 ? $"!! {stock}" : "XX 0";
                result += $" | {display,7}";
                if (stock >= item.Quantity) itemFulfillable = true;
            }

            if (!itemFulfillable && totalAcrossWarehouses >= item.Quantity)
            {
                result += " | SPLIT OK";
                allFromOneWarehouse = false;
            }
            else if (!itemFulfillable)
            {
                result += " | SHORTAGE";
                allFromOneWarehouse = false;
            }
            else
            {
                result += " | OK";
            }
            result += "\n";
        }

        result += new string('=', 70) + "\n";
        return result;
    }

    [Description("Finds the optimal warehouse(s) to fulfill an order from, considering stock, " +
                 "shipping cost, and transit time. Compares single-warehouse vs split-shipment " +
                 "costs and recommends the cheapest option. Always picks the lowest total cost.")]
    public static string OptimizeFulfillment(
        [Description("The order ID")] string orderId)
    {
        var order = OrderDatabase.GetOrder(orderId);
        if (order is null) return $"ERROR: Order {orderId} not found.";

        var warehouses = WarehouseDatabase.GetAllWarehouses();
        decimal totalWeight = order.Items.Sum(i => i.Weight * i.Quantity);

        var result = $"FULFILLMENT OPTIMIZATION -- {orderId}\n";
        result += $"Destination: {order.ShippingCity}, {order.ShippingCountry}\n";
        result += $"Total weight: {totalWeight:F1} kg\n";
        result += new string('=', 65) + "\n\n";

        // === Option A: Single warehouse ===
        var singleOptions = new List<(string whId, string whName, decimal cost, int days)>();

        foreach (var wh in warehouses)
        {
            bool canFulfillAll = order.Items.All(item =>
                WarehouseDatabase.GetStock(wh.WarehouseId, item.Sku) >= item.Quantity);

            if (canFulfillAll)
            {
                decimal rate = WarehouseDatabase.GetShippingRate(wh.WarehouseId, order.ShippingCountry);
                decimal cost = totalWeight * rate;
                int days = WarehouseDatabase.GetTransitDays(wh.WarehouseId, order.ShippingCountry);
                singleOptions.Add((wh.WarehouseId, wh.WarehouseName, cost, days));
            }
        }

        result += "OPTION A: SINGLE WAREHOUSE\n";
        if (singleOptions.Any())
        {
            foreach (var opt in singleOptions.OrderBy(o => o.cost))
            {
                result += $"  {opt.whId} ({opt.whName}): ${opt.cost:F2} | {opt.days} days\n";
            }
            var bestSingle = singleOptions.OrderBy(o => o.cost).First();
            result += $"  >> Best single: {bestSingle.whId} at ${bestSingle.cost:F2}, {bestSingle.days} days\n";
        }
        else
        {
            result += "  No single warehouse can fulfill all items.\n";
        }

        // === Option B: Split shipment (nearest warehouse per item, split if needed) ===
        result += "\nOPTION B: SPLIT SHIPMENT (cheapest per item)\n";
        decimal splitTotalCost = 0;
        int splitMaxDays = 0;
        var splitPlan = new List<string>();
        bool splitPossible = true;

        foreach (var item in order.Items)
        {
            int needed = item.Quantity;
            decimal itemWeight = item.Weight;

            // Find cheapest warehouse that has enough stock for this item
            var bestWh = warehouses
                .Where(wh => WarehouseDatabase.GetStock(wh.WarehouseId, item.Sku) >= needed)
                .Select(wh => new
                {
                    wh.WarehouseId,
                    wh.WarehouseName,
                    Cost = needed * itemWeight * WarehouseDatabase.GetShippingRate(wh.WarehouseId, order.ShippingCountry),
                    Days = WarehouseDatabase.GetTransitDays(wh.WarehouseId, order.ShippingCountry)
                })
                .OrderBy(x => x.Cost)
                .FirstOrDefault();

            if (bestWh != null)
            {
                splitTotalCost += bestWh.Cost;
                splitMaxDays = Math.Max(splitMaxDays, bestWh.Days);
                splitPlan.Add($"  {needed}x {item.Sku} -> {bestWh.WarehouseId} ({bestWh.WarehouseName}): ${bestWh.Cost:F2}, {bestWh.Days} days");
            }
            else
            {
                // No single warehouse has enough -- try combining warehouses
                var remaining = needed;
                decimal itemCost = 0;
                int itemMaxDays = 0;
                var itemSources = new List<string>();

                foreach (var wh in warehouses.OrderBy(wh =>
                    WarehouseDatabase.GetShippingRate(wh.WarehouseId, order.ShippingCountry)))
                {
                    int available = WarehouseDatabase.GetStock(wh.WarehouseId, item.Sku);
                    if (available <= 0 || remaining <= 0) continue;

                    int take = Math.Min(available, remaining);
                    decimal partCost = take * itemWeight * WarehouseDatabase.GetShippingRate(wh.WarehouseId, order.ShippingCountry);
                    int partDays = WarehouseDatabase.GetTransitDays(wh.WarehouseId, order.ShippingCountry);

                    itemCost += partCost;
                    itemMaxDays = Math.Max(itemMaxDays, partDays);
                    itemSources.Add($"{take}x from {wh.WarehouseId}");
                    remaining -= take;
                }

                if (remaining > 0)
                {
                    splitPlan.Add($"  {needed}x {item.Sku} -> SHORTAGE (missing {remaining} units)");
                    splitPossible = false;
                }
                else
                {
                    splitTotalCost += itemCost;
                    splitMaxDays = Math.Max(splitMaxDays, itemMaxDays);
                    splitPlan.Add($"  {needed}x {item.Sku} -> SPLIT: {string.Join(" + ", itemSources)}: ${itemCost:F2}, {itemMaxDays} days");
                }
            }
        }

        foreach (var line in splitPlan)
            result += line + "\n";

        if (splitPossible)
            result += $"  >> Split total: ${splitTotalCost:F2} | Max {splitMaxDays} days\n";

        // === RECOMMENDATION ===
        result += "\n=== RECOMMENDATION ===\n";

        if (singleOptions.Any() && splitPossible)
        {
            var bestSingle = singleOptions.OrderBy(o => o.cost).First();
            if (bestSingle.cost <= splitTotalCost)
            {
                result += $"BEST: SINGLE WAREHOUSE -> {bestSingle.whId} ({bestSingle.whName})\n";
                result += $"Cost: ${bestSingle.cost:F2} | ETA: {bestSingle.days} days | Arrive: {DateTime.UtcNow.AddDays(bestSingle.days):dd MMM yyyy}\n";
                result += $"Reason: Single shipment is cheaper (${bestSingle.cost:F2}) than split (${splitTotalCost:F2})\n";
            }
            else
            {
                result += $"BEST: SPLIT SHIPMENT\n";
                result += $"Cost: ${splitTotalCost:F2} | Max ETA: {splitMaxDays} days\n";
                result += $"Reason: Split (${splitTotalCost:F2}) is cheaper than best single warehouse (${bestSingle.cost:F2})\n";
                foreach (var line in splitPlan)
                    result += line + "\n";
            }
        }
        else if (singleOptions.Any())
        {
            var bestSingle = singleOptions.OrderBy(o => o.cost).First();
            result += $"BEST: SINGLE WAREHOUSE -> {bestSingle.whId} ({bestSingle.whName})\n";
            result += $"Cost: ${bestSingle.cost:F2} | ETA: {bestSingle.days} days | Arrive: {DateTime.UtcNow.AddDays(bestSingle.days):dd MMM yyyy}\n";
        }
        else if (splitPossible)
        {
            result += $"BEST: SPLIT SHIPMENT (no single warehouse has all items)\n";
            result += $"Cost: ${splitTotalCost:F2} | Max ETA: {splitMaxDays} days\n";
            foreach (var line in splitPlan)
                result += line + "\n";
        }
        else
        {
            result += "CANNOT FULFILL -- some items have global shortage. Backorder required.\n";
        }

        return result;
    }

    [Description("Reserves stock in a specific warehouse for an order. " +
                 "Reduces available inventory. Returns confirmation or failure.")]
    public static string ReserveStock(
        [Description("The warehouse ID (e.g. WH-IST)")] string warehouseId,
        [Description("The SKU to reserve")] string sku,
        [Description("The quantity to reserve")] int quantity)
    {
        bool success = WarehouseDatabase.ReserveStock(warehouseId, sku, quantity);
        int remaining = WarehouseDatabase.GetStock(warehouseId, sku);

        return success
            ? $"RESERVED: {quantity}x {sku} at {warehouseId}. Remaining stock: {remaining}"
            : $"RESERVATION FAILED: Insufficient stock for {quantity}x {sku} at {warehouseId}. Available: {remaining}";
    }
}