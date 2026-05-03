using System.ComponentModel;
using FoundryCommerceDemo.Data;

namespace FoundryCommerceDemo.Tools;

public static class FraudTools
{
    [Description("Analyzes geographic consistency of an order. Compares shipping country, " +
                 "payment card issuing country, and IP geolocation. Returns a geo-risk assessment " +
                 "with specific flags for each mismatch detected.")]
    public static string AnalyzeGeoConsistency(
        [Description("The order ID to analyze, e.g. ORD-50001")] string orderId)
    {
        var order = OrderDatabase.GetOrder(orderId);
        if (order is null) return $"ERROR: Order {orderId} not found.";

        var flags = new List<string>();
        int riskPoints = 0;

        // Card country vs shipping country
        if (order.PaymentCardCountry != order.ShippingCountry &&
            order.PaymentMethod == "Credit Card")
        {
            flags.Add($"CARD/SHIPPING MISMATCH: Card issued in {order.PaymentCardCountry}, " +
                       $"shipping to {order.ShippingCountry}");
            riskPoints += 30;
        }

        // IP country vs shipping country
        if (order.IpCountry != order.ShippingCountry)
        {
            flags.Add($"IP/SHIPPING MISMATCH: IP from {order.IpCountry}, " +
                       $"shipping to {order.ShippingCountry}");
            riskPoints += 25;
        }

        // IP country vs card country
        if (order.IpCountry != order.PaymentCardCountry &&
            order.PaymentMethod == "Credit Card")
        {
            flags.Add($"IP/CARD MISMATCH: IP from {order.IpCountry}, " +
                       $"card from {order.PaymentCardCountry}");
            riskPoints += 20;
        }

        // High-risk shipping destinations
        var highRiskCountries = new[] { "NG", "GH", "PK", "BY" };
        if (highRiskCountries.Contains(order.ShippingCountry))
        {
            flags.Add($"HIGH-RISK DESTINATION: {order.ShippingCountry} is on watchlist");
            riskPoints += 15;
        }

        // Disposable email check
        var disposableDomains = new[] { "tempmail.xyz", "throwaway.io", "fakebox.net" };
        if (disposableDomains.Any(d => order.CustomerEmail.EndsWith(d)))
        {
            flags.Add("DISPOSABLE EMAIL: Temporary email service detected");
            riskPoints += 20;
        }

        string riskLevel = riskPoints switch
        {
            0 => "LOW",
            <= 25 => "MEDIUM",
            <= 50 => "HIGH",
            _ => "CRITICAL"
        };

        return $"""
            GEO-CONSISTENCY ANALYSIS — {orderId}
            ═══════════════════════════════════════
            Shipping:  {order.ShippingCity}, {order.ShippingCountry}
            Card From: {order.PaymentCardCountry}
            IP From:   {order.IpCountry} ({order.IpAddress})
            Email:     {order.CustomerEmail}
            ───────────────────────────────────────
            Risk Points: {riskPoints}/100
            Geo Risk Level: {riskLevel}
            Flags: {(flags.Count == 0 ? "None — all locations consistent" : "\n" + string.Join("\n", flags.Select(f => $"  ⚠ {f}")))}
            ═══════════════════════════════════════
            """;
    }

    [Description("Checks order velocity — how many orders the customer has placed recently. " +
                 "High velocity from new accounts or with high-value items indicates potential fraud.")]
    public static string CheckOrderVelocity(
        [Description("The order ID to check")] string orderId)
    {
        var order = OrderDatabase.GetOrder(orderId);
        if (order is null) return $"ERROR: Order {orderId} not found.";

        int recentOrders = OrderDatabase.GetRecentOrderCount(order.CustomerId);
        bool isNewDevice = order.DeviceFingerprint.Contains("NEW");
        bool isHighValue = order.TotalAmount > 5000m;

        var flags = new List<string>();
        int riskPoints = 0;

        if (recentOrders > 5)
        {
            flags.Add($"HIGH VELOCITY: {recentOrders} orders in last 24h (threshold: 5)");
            riskPoints += 35;
        }
        else if (recentOrders > 3)
        {
            flags.Add($"ELEVATED VELOCITY: {recentOrders} orders in last 24h");
            riskPoints += 15;
        }

        if (isNewDevice)
        {
            flags.Add("NEW DEVICE: First time seeing this device fingerprint");
            riskPoints += 20;
        }

        if (isHighValue && isNewDevice)
        {
            flags.Add("COMBO FLAG: High-value order from unknown device");
            riskPoints += 25;
        }

        // Check for GPU/high-resale items
        var highResaleSkus = new[] { "GPU-RTX5090" };
        var resaleItems = order.Items.Where(i => highResaleSkus.Contains(i.Sku)).ToList();
        if (resaleItems.Any())
        {
            flags.Add($"HIGH-RESALE ITEMS: {string.Join(", ", resaleItems.Select(i => $"{i.Quantity}x {i.ProductName}"))}");
            riskPoints += 20;
        }

        return $"""
            VELOCITY & BEHAVIOR ANALYSIS — {orderId}
            ═══════════════════════════════════════
            Customer: {order.CustomerName} ({order.CustomerId})
            Tier: {order.CustomerTier}
            Recent Orders (24h): {recentOrders}
            Device: {(isNewDevice ? "NEW (first seen)" : "Known device")}
            Order Value: {order.TotalAmount:C}
            ───────────────────────────────────────
            Risk Points: {riskPoints}/100
            Flags: {(flags.Count == 0 ? "None — normal behavior" : "\n" + string.Join("\n", flags.Select(f => $"  ⚠ {f}")))}
            ═══════════════════════════════════════
            """;
    }

    [Description("Produces a final fraud verdict combining geo-analysis and velocity checks. " +
                 "Returns APPROVE, REVIEW (manual), or BLOCK with a confidence score.")]
    public static string GenerateFraudVerdict(
        [Description("The order ID")] string orderId,
        [Description("Total risk points from geo analysis (0-100)")] int geoRiskPoints,
        [Description("Total risk points from velocity analysis (0-100)")] int velocityRiskPoints)
    {
        var order = OrderDatabase.GetOrder(orderId);
        if (order is null) return $"ERROR: Order {orderId} not found.";

        int totalRisk = geoRiskPoints + velocityRiskPoints;

        // Tier-based adjustment
        int tierDiscount = order.CustomerTier switch
        {
            "Platinum" => 30,
            "Gold" => 15,
            _ => 0
        };
        int adjustedRisk = Math.Max(0, totalRisk - tierDiscount);

        string verdict;
        string action;
        if (adjustedRisk >= 60)
        {
            verdict = "BLOCK";
            action = "Order held. Escalate to fraud team. Do NOT fulfill.";
            OrderDatabase.UpdateStatus(orderId, "Blocked - Fraud");
        }
        else if (adjustedRisk >= 30)
        {
            verdict = "MANUAL REVIEW";
            action = "Order queued for manual review. 3D Secure challenge recommended.";
            OrderDatabase.UpdateStatus(orderId, "Under Review");
        }
        else
        {
            verdict = "APPROVE";
            action = "Order cleared for fulfillment. Standard monitoring applies.";
            OrderDatabase.UpdateStatus(orderId, "Fraud Cleared");
        }

        return $"""
            ╔══════════════════════════════════════════════╗
            ║         FRAUD ASSESSMENT — {orderId}        ║
            ╠══════════════════════════════════════════════╣
            ║  Verdict:        {verdict,-28} ║
            ║  Geo Risk:       {geoRiskPoints}/100{"",-23} ║
            ║  Velocity Risk:  {velocityRiskPoints}/100{"",-23} ║
            ║  Total Raw:      {totalRisk}/200{"",-23} ║
            ║  Tier Discount:  -{tierDiscount} ({order.CustomerTier}){"",-17} ║
            ║  Adjusted Score: {adjustedRisk}/200{"",-23} ║
            ╠══════════════════════════════════════════════╣
            ║  Action: {action,-35} ║
            ╚══════════════════════════════════════════════╝
            """;
    }
}