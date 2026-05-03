using System.Diagnostics;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using FoundryCommerceDemo.Tools;
using FoundryCommerceDemo.Agents;
using FoundryCommerceDemo.Data;

// =================================================================
// CONFIGURATION
// =================================================================

var endpoint = Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT")
    ?? "https://<your-resource>.services.ai.azure.com/api/projects/<your-project>";
var model = Environment.GetEnvironmentVariable("FOUNDRY_MODEL_NAME")
    ?? "gpt-4o-mini";

var credential = new DefaultAzureCredential();
var projectClient = new AIProjectClient(new Uri(endpoint), credential);

// =================================================================
// ORDER STATUS TRACKING
// =================================================================

var orderStatuses = new Dictionary<string, string>();

// =================================================================
// CONSOLE HELPERS
// =================================================================

void WriteColor(string text, ConsoleColor color)
{
    Console.ForegroundColor = color;
    Console.Write(text);
    Console.ResetColor();
}

void WriteLineColor(string text, ConsoleColor color)
{
    Console.ForegroundColor = color;
    Console.WriteLine(text);
    Console.ResetColor();
}

void WriteBanner()
{
    Console.Clear();
    WriteLineColor(@"
    +===================================================================+
    |                                                                   |
    |   ███████╗ ██████╗ ██╗   ██╗███╗   ██╗██████╗ ██████╗ ██╗   ██╗  |
    |   ██╔════╝██╔═══██╗██║   ██║████╗  ██║██╔══██╗██╔══██╗╚██╗ ██╔╝  |
    |   █████╗  ██║   ██║██║   ██║██╔██╗ ██║██║  ██║██████╔╝ ╚████╔╝   |
    |   ██╔══╝  ██║   ██║██║   ██║██║╚██╗██║██║  ██║██╔══██╗  ╚██╔╝    |
    |   ██║     ╚██████╔╝╚██████╔╝██║ ╚████║██████╔╝██║  ██║   ██║     |
    |   ╚═╝      ╚═════╝  ╚═════╝ ╚═╝  ╚═══╝╚═════╝ ╚═╝  ╚═╝   ╚═╝   |
    |                                                                   |
    |   Enterprise E-Commerce Operations Center                         |
    |   Powered by Microsoft Agent Framework v1.0                       |
    |                                                                   |
    |   Fraud Detection  ->  Fulfillment  ->  Customer Comms            |
    |                                                                   |
    +===================================================================+
", ConsoleColor.DarkCyan);
}

void WriteStep(int step, string title, ConsoleColor color)
{
    Console.WriteLine();
    Console.ForegroundColor = color;
    Console.WriteLine($"  +-----------------------------------------------------------+");
    Console.WriteLine($"  |  STEP {step}: {title,-49} |");
    Console.WriteLine($"  +-----------------------------------------------------------+");
    Console.ResetColor();
    Console.WriteLine();
}

void WriteProcessingHeader(string orderId, string name, decimal amount)
{
    Console.WriteLine();
    WriteLineColor("  +===========================================================+", ConsoleColor.White);
    Console.Write("  |  PROCESSING: ");
    WriteColor(orderId, ConsoleColor.Cyan);
    Console.Write(" -- ");
    WriteColor(name, ConsoleColor.White);
    Console.Write(" -- ");
    var amountStr = amount.ToString("C");
    Console.WriteLine($"{amountStr,-20} |");
    WriteLineColor("  +===========================================================+", ConsoleColor.White);
}

void WriteResult(string label, double seconds, ConsoleColor color)
{
    string timeStr = $"({seconds:F1}s)";
    Console.WriteLine();
    Console.ForegroundColor = color;
    Console.WriteLine($"  +---------------------------------------------------+");
    Console.Write($"  |  {label,-40}");
    Console.ForegroundColor = ConsoleColor.DarkYellow;
    Console.WriteLine($" {timeStr,8} |");
    Console.ForegroundColor = color;
    Console.WriteLine($"  +---------------------------------------------------+");
    Console.ResetColor();
}

void WriteResultNoTime(string label, ConsoleColor color)
{
    Console.WriteLine();
    Console.ForegroundColor = color;
    Console.WriteLine($"  +---------------------------------------------------+");
    Console.WriteLine($"  |  {label,-49} |");
    Console.WriteLine($"  +---------------------------------------------------+");
    Console.ResetColor();
}

void WriteAgentResponse(string response)
{
    var lines = response.Split('\n');
    foreach (var line in lines)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("    | ");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine(line.TrimEnd());
    }
    Console.ResetColor();
}

void WriteLoading(string message)
{
    WriteLineColor($"    ... {message}", ConsoleColor.DarkYellow);
}

void WriteSummary(string orderId, double totalSeconds, string verdict)
{
    Console.WriteLine();
    WriteLineColor("  +---------------------------------------------------+", ConsoleColor.White);
    Console.Write("  |  SUMMARY: ");
    WriteColor(orderId, ConsoleColor.Cyan);
    Console.Write("  Verdict: ");
    var vc = verdict == "BLOCKED" ? ConsoleColor.Red : ConsoleColor.Green;
    WriteColor($"{verdict,-10}", vc);
    Console.ForegroundColor = ConsoleColor.DarkYellow;
    Console.WriteLine($" Total: {totalSeconds:F1}s  |");
    Console.ResetColor();
    WriteLineColor("  +---------------------------------------------------+", ConsoleColor.White);
}

// =================================================================
// INTERACTIVE ORDER SELECTOR (Arrow Keys + Status)
// =================================================================

string? SelectOrder()
{
    var orders = OrderDatabase.GetAllOrders();
    var menuItems = new List<(string id, string display)>();

    foreach (var o in orders)
    {
        var tier = o.CustomerTier switch
        {
            "Platinum" => "PLATINUM ",
            "Gold" => "GOLD     ",
            _ => "STANDARD "
        };
        var dest = $"{o.ShippingCity}, {o.ShippingCountry}";

        string status = "         ";
        if (orderStatuses.TryGetValue(o.OrderId, out var st))
        {
            status = st switch
            {
                "DONE" => " [DONE]  ",
                "BLOCKED" => "[BLOCKED]",
                "REVIEW" => " [REVIEW]",
                _ => "         "
            };
        }

        menuItems.Add((o.OrderId,
            $"  {o.OrderId}   {o.CustomerName,-20}  {o.TotalAmount,12:C}   {dest,-16}  {tier} {status}"));
    }

    menuItems.Add(("all", "  >> PROCESS ALL ORDERS                                                                "));
    menuItems.Add(("quit", "  >> QUIT                                                                              "));

    int selected = 0;
    int totalMenuLines = menuItems.Count + 6;

    DrawMenu();

    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                selected = (selected - 1 + menuItems.Count) % menuItems.Count;
                break;
            case ConsoleKey.DownArrow:
                selected = (selected + 1) % menuItems.Count;
                break;
            case ConsoleKey.Enter:
                var choice = menuItems[selected].id;
                return choice == "quit" ? null : choice;
            case ConsoleKey.Escape:
            case ConsoleKey.Q:
                return null;
            default:
                continue;
        }

        Console.CursorTop -= totalMenuLines;
        DrawMenu();
    }

    void DrawMenu()
    {
        string line = "  +" + new string('-', 96) + "+";
        string header = string.Format("  | {0,-10} | {1,-20} | {2,12} | {3,-16} | {4,-9} | {5,-9} |",
            "ORDER", "CUSTOMER", "AMOUNT", "DESTINATION", "TIER", "STATUS");

        Console.WriteLine();
        WriteLineColor(line, ConsoleColor.DarkGray);
        WriteLineColor(header, ConsoleColor.DarkGray);
        WriteLineColor(line, ConsoleColor.DarkGray);

        for (int i = 0; i < menuItems.Count; i++)
        {
            if (i == selected)
            {
                Console.BackgroundColor = ConsoleColor.DarkCyan;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($" > {menuItems[i].display}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"   {menuItems[i].display}");
                Console.ResetColor();
            }
        }

        WriteLineColor(line, ConsoleColor.DarkGray);
        WriteLineColor("    [Up/Down] Navigate    [Enter] Select    [Q] Quit", ConsoleColor.DarkGray);
    }
}

// =================================================================
// CREATE AGENTS
// =================================================================

AIAgent fraudAgent = projectClient.AsAIAgent(
    model: model,
    name: "FraudDetectionAgent",
    instructions: AgentInstructions.FraudDetection,
    tools:
    [
        AIFunctionFactory.Create(FraudTools.AnalyzeGeoConsistency),
        AIFunctionFactory.Create(FraudTools.CheckOrderVelocity),
        AIFunctionFactory.Create(FraudTools.GenerateFraudVerdict),
    ]);

AIAgent inventoryAgent = projectClient.AsAIAgent(
    model: model,
    name: "InventoryAgent",
    instructions: AgentInstructions.Inventory,
    tools:
    [
        AIFunctionFactory.Create(InventoryTools.CheckStockAvailability),
        AIFunctionFactory.Create(InventoryTools.OptimizeFulfillment),
        AIFunctionFactory.Create(InventoryTools.ReserveStock),
    ]);

// FulfillmentAgent kept for agent-as-tool demo if needed
AIAgent fulfillmentAgent = projectClient.AsAIAgent(
    model: model,
    name: "FulfillmentAgent",
    instructions: AgentInstructions.Fulfillment,
    tools: [inventoryAgent.AsAIFunction()]);

AIAgent customerAgent = projectClient.AsAIAgent(
    model: model,
    name: "CustomerCommsAgent",
    instructions: AgentInstructions.CustomerComms,
    tools:
    [
        AIFunctionFactory.Create(CustomerTools.GetOrderForCommunication),
        AIFunctionFactory.Create(CustomerTools.LogCommunication),
    ]);

// =================================================================
// PROCESS ORDER -- Full pipeline with timing
// =================================================================

async Task ProcessOrder(string orderId)
{
    var order = OrderDatabase.GetOrder(orderId);
    if (order is null)
    {
        WriteResultNoTime($"[X] Order {orderId} not found", ConsoleColor.Red);
        return;
    }

    var totalSw = Stopwatch.StartNew();
    WriteProcessingHeader(orderId, order.CustomerName, order.TotalAmount);

    // -- STEP 1: Fraud Detection ------------------------------------
    WriteStep(1, "FRAUD DETECTION", ConsoleColor.Red);
    WriteLoading("Analyzing geo-consistency, velocity & risk...");

    var sw = Stopwatch.StartNew();
    var fraudResult = await fraudAgent.RunAsync(
        $"Analyze order {orderId} for fraud. Run geo-consistency, velocity check, " +
        $"and generate the final verdict.");
    sw.Stop();
    WriteAgentResponse(fraudResult.ToString());

    bool isBlocked = fraudResult.ToString()
        .Contains("BLOCK", StringComparison.OrdinalIgnoreCase);
    bool needsReview = fraudResult.ToString()
        .Contains("MANUAL REVIEW", StringComparison.OrdinalIgnoreCase);

    if (isBlocked)
    {
        WriteResult("[X] BLOCKED -- Escalated to fraud team", sw.Elapsed.TotalSeconds, ConsoleColor.Red);

        WriteStep(0, "CUSTOMER NOTIFICATION (HOLD)", ConsoleColor.DarkRed);
        WriteLoading("Drafting verification email...");

        var sw2 = Stopwatch.StartNew();
        var holdComms = await customerAgent.RunAsync(
            $"Draft a professional email to the customer for order {orderId}. " +
            $"The order requires additional verification before it can be processed. " +
            $"Do NOT mention fraud or specific checks. Be helpful and professional. " +
            $"Then log the communication.");
        sw2.Stop();
        WriteAgentResponse(holdComms.ToString());
        WriteResult("[!] Hold notification sent -- pipeline stopped", sw2.Elapsed.TotalSeconds, ConsoleColor.DarkRed);

        totalSw.Stop();
        orderStatuses[orderId] = "BLOCKED";
        WriteSummary(orderId, totalSw.Elapsed.TotalSeconds, "BLOCKED");
        return;
    }

    if (needsReview)
    {
        WriteResult("[?] MANUAL REVIEW -- Proceeding with caution", sw.Elapsed.TotalSeconds, ConsoleColor.Yellow);
        orderStatuses[orderId] = "REVIEW";
    }
    else
    {
        WriteResult("[OK] APPROVED -- Cleared for fulfillment", sw.Elapsed.TotalSeconds, ConsoleColor.Green);
    }

    // -- STEP 2: Fulfillment (direct InventoryAgent for speed) ------
    WriteStep(2, "FULFILLMENT PLANNING", ConsoleColor.Cyan);
    WriteLoading("Checking stock across warehouses (IST, FRA, DXB)...");

    sw = Stopwatch.StartNew();
    var fulfillmentResult = await inventoryAgent.RunAsync(
        $"Process fulfillment for order {orderId}. " +
        $"Destination: {order.ShippingCity}, {order.ShippingCountry}. " +
        $"Items: {string.Join(", ", order.Items.Select(i => $"{i.Quantity}x {i.Sku}"))}. " +
        $"Step 1: Check stock availability. " +
        $"Step 2: Find optimal fulfillment (cheapest shipping). " +
        $"Step 3: Reserve stock at the recommended warehouse(s). " +
        $"Execute all three steps now. Be concise.");
    sw.Stop();
    WriteAgentResponse(fulfillmentResult.ToString());
    WriteResult("[OK] Inventory reserved -- shipment planned", sw.Elapsed.TotalSeconds, ConsoleColor.Cyan);

    // -- STEP 3: Customer Communication -----------------------------
    WriteStep(3, "CUSTOMER COMMUNICATION", ConsoleColor.Green);
    WriteLoading("Drafting confirmation email...");

    sw = Stopwatch.StartNew();
    var commsResult = await customerAgent.RunAsync(
        $"""
        Draft an order confirmation email for {orderId}.
        Customer: {order.CustomerName}, Tier: {order.CustomerTier}.
        Shipping to: {order.ShippingCity}, {order.ShippingCountry}.

        Here is the fulfillment plan:
        {fulfillmentResult}

        Include shipping ETAs. If it's a split shipment, explain it clearly.
        Match tone to the customer's tier:
        - Platinum = VIP white-glove, warm and personal
        - Gold = friendly and appreciative
        - Standard = professional and clear
        Do NOT mention warehouse IDs or shipping costs.
        Then log the communication.
        """);
    sw.Stop();
    WriteAgentResponse(commsResult.ToString());
    WriteResult("[OK] Confirmation sent -- order complete", sw.Elapsed.TotalSeconds, ConsoleColor.Green);

    totalSw.Stop();
    orderStatuses[orderId] = "DONE";
    WriteSummary(orderId, totalSw.Elapsed.TotalSeconds, "APPROVED");
}

// =================================================================
// MAIN LOOP
// =================================================================

WriteBanner();

while (true)
{
    var choice = SelectOrder();

    if (choice is null)
        break;

    var orderIds = choice == "all"
        ? OrderDatabase.GetAllOrders().Select(o => o.OrderId).ToList()
        : new List<string> { choice };

    foreach (var id in orderIds)
    {
        await ProcessOrder(id);
    }

    Console.WriteLine();
    WriteLineColor("  ===========================================================", ConsoleColor.DarkGray);
    WriteLineColor("  [OK] Ready for next order -- agents standing by", ConsoleColor.DarkCyan);
    WriteLineColor("  ===========================================================", ConsoleColor.DarkGray);
    Console.WriteLine("\n  Press any key to continue...");
    Console.ReadKey(intercept: true);
    WriteBanner();
}

Console.Clear();
WriteLineColor(@"
  +=========================================================+
  |                                                         |
  |  Thank you for using Foundry Commerce Demo!             |
  |                                                         |
  |  The model REASONS -- your code COMPUTES                |
  |  Zero hallucination in business logic.                  |
  |                                                         |
  |  github.com/microsoft/agent-framework                   |
  |                                                         |
  +=========================================================+
", ConsoleColor.DarkCyan);