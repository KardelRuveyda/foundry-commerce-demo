using System.ComponentModel;
using FoundryCommerceDemo.Data;

namespace FoundryCommerceDemo.Tools;

public static class CustomerTools
{
    [Description("Retrieves order details for composing customer-facing communications. " +
                 "Returns order info suitable for customer emails — no internal data exposed.")]
    public static string GetOrderForCommunication(
        [Description("The order ID")] string orderId)
    {
        var order = OrderDatabase.GetOrder(orderId);
        if (order is null) return $"ERROR: Order {orderId} not found.";

        var itemList = string.Join("\n",
            order.Items.Select(i => $"    • {i.Quantity}x {i.ProductName} — {(i.Price * i.Quantity):C}"));

        return $"""
            ORDER DETAILS (customer-safe)
            ─────────────────────────────────
            Order: {order.OrderId}
            Customer: {order.CustomerName}
            Email: {order.CustomerEmail}
            Tier: {order.CustomerTier}
            Items:
            {itemList}
            Total: {order.TotalAmount:C}
            Shipping To: {order.ShippingCity}, {order.ShippingCountry}
            Status: {order.Status}
            Placed: {order.PlacedAt:dd MMM yyyy HH:mm} UTC
            ─────────────────────────────────
            """;
    }

    [Description("Logs a customer communication event. Records what was sent, through which " +
                 "channel, and when. Used for audit trails and CRM tracking.")]
    public static string LogCommunication(
        [Description("The order ID")] string orderId,
        [Description("Channel: email, sms, or push")] string channel,
        [Description("Brief summary of what was communicated")] string summary)
    {
        return $"""
            ✅ COMMUNICATION LOGGED
            Order: {orderId}
            Channel: {channel.ToUpper()}
            Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
            Summary: {summary}
            Status: Queued for delivery
            """;
    }
}