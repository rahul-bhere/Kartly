using System.Text.Json;
using Kartly.Application.DTOs.Assistant;
using Kartly.Application.DTOs.Orders;
using Kartly.Application.DTOs.Products;
using Kartly.Application.Interfaces;

namespace Kartly.Application.Services;

// Takes a plain-English message and asks the LLM (via ILlmClient) which -
// if any - of a small, ROLE-DEPENDENT set of tools to call, executes the
// matching real operation against IProductService / IUserService /
// IOrderService (the SAME services the REST controllers use), and
// returns a natural-language confirmation.
//
// WHY TWO DIFFERENT TOOL SETS (THIS IS THE ACTUAL SECURITY BOUNDARY):
// An admin's chat can create/update/delete products and manage anyone's
// orders. A shopper's chat can only look up products, check THEIR OWN
// orders, and cancel THEIR OWN order - the exact same ownership rule
// OrdersController enforces on the REST endpoints. The LLM never gets a
// choice: BuildShopperTools()/BuildAdminTools() below simply never hand
// a write-everything tool to a non-admin request.
public class AssistantService : IAssistantService
{
    private readonly ILlmClient _llmClient;
    private readonly IProductService _productService;
    private readonly IUserService _userService;
    private readonly IOrderService _orderService;

    private const string AdminSystemPrompt =
        """
        You are Kartly's admin assistant. You help the admin manage products,
        look up users, and manage orders (including cancelling or changing an
        order's status) by calling the tools provided.

        Rules:
        - Only call a tool when the admin's message clearly asks for an action
          a tool covers. For general questions or small talk, reply in text
          with no tool call.
        - When updating or deleting a product, prefer "id" if given, otherwise
          "title" and the closest match will be used.
        - Prices are in US dollars.
        - After a tool result comes back, summarize what happened in one or
          two friendly sentences. Do not repeat raw JSON back to the admin.
        """;

    private const string ShopperSystemPrompt =
        """
        You are Kartly's shopping assistant. You can look up products in the
        admin-managed catalog, check the shopper's own past orders, and
        cancel one of their own orders if it's still cancellable - by calling
        the tools provided. You cannot create, edit, or delete products, and
        you cannot see or touch any other shopper's data.

        Rules:
        - Only call a tool when clearly asked for something it covers.
        - After a tool result comes back, summarize what happened in one or
          two friendly sentences. Do not repeat raw JSON back to the shopper.
        """;

    public AssistantService(
        ILlmClient llmClient,
        IProductService productService,
        IUserService userService,
        IOrderService orderService)
    {
        _llmClient = llmClient;
        _productService = productService;
        _userService = userService;
        _orderService = orderService;
    }

    public async Task<ChatResponseDto> ChatAsync(Guid userId, bool isAdmin, ChatRequestDto request)
    {
        var messages = request.History
            .Select(h => new LlmMessage { Role = h.Role, Text = h.Content })
            .ToList();
        messages.Add(new LlmMessage { Role = "user", Text = request.Message });

        var systemPrompt = isAdmin ? AdminSystemPrompt : ShopperSystemPrompt;
        var tools = isAdmin ? BuildAdminTools() : BuildShopperTools();

        var firstResponse = await _llmClient.SendAsync(systemPrompt, messages, tools);

        if (firstResponse.ToolCall is null)
        {
            return new ChatResponseDto
            {
                Reply = firstResponse.TextContent ?? "I'm not sure how to help with that.",
                ActionsPerformed = new List<string>()
            };
        }

        var (resultJson, actionLog) = await ExecuteToolAsync(userId, isAdmin, firstResponse.ToolCall);

        messages.Add(new LlmMessage { Role = "assistant", Text = string.Empty, ToolCall = firstResponse.ToolCall });
        messages.Add(new LlmMessage { Role = "user", Text = string.Empty, ToolResultJson = resultJson });

        var secondResponse = await _llmClient.SendAsync(systemPrompt, messages, tools);

        return new ChatResponseDto
        {
            Reply = secondResponse.TextContent ?? "Done.",
            ActionsPerformed = actionLog is null ? new List<string>() : new List<string> { actionLog }
        };
    }

    private async Task<(string resultJson, string? actionLog)> ExecuteToolAsync(Guid userId, bool isAdmin, LlmToolCall toolCall)
    {
        using var input = JsonDocument.Parse(string.IsNullOrWhiteSpace(toolCall.InputJson) ? "{}" : toolCall.InputJson);
        var root = input.RootElement;

        try
        {
            switch (toolCall.Name)
            {
                case "list_products":
                {
                    var query = root.TryGetProperty("query", out var q) ? q.GetString() : null;
                    var category = root.TryGetProperty("category", out var c) ? c.GetString() : null;
                    var products = await _productService.GetAllAsync(query, category);
                    return (JsonSerializer.Serialize(products.Take(15)), $"Looked up products (found {products.Count}).");
                }

                case "list_my_orders":
                {
                    if (isAdmin) return DenyForRole("list_my_orders", "admin");
                    var orders = await _orderService.GetMyOrdersAsync(userId);
                    return (JsonSerializer.Serialize(orders.Take(20)), $"Looked up your orders (found {orders.Count}).");
                }

                case "cancel_my_order":
                {
                    if (isAdmin) return DenyForRole("cancel_my_order", "admin");
                    if (!root.TryGetProperty("orderId", out var idProp) || !Guid.TryParse(idProp.GetString(), out var orderId))
                        return (JsonSerializer.Serialize(new { error = "A valid orderId is required." }), null);

                    var cancelled = await _orderService.CancelOrderAsync(userId, orderId, isAdmin: false);
                    return (JsonSerializer.Serialize(cancelled), $"Cancelled order {cancelled.Id}.");
                }

                case "create_product":
                {
                    if (!isAdmin) return DenyForRole("create_product", "shopper");
                    var dto = new CreateProductDto
                    {
                        Title = GetString(root, "title"),
                        Description = GetString(root, "description", "No description provided."),
                        Price = GetDecimal(root, "price"),
                        DiscountPercentage = GetDecimal(root, "discountPercentage", 0),
                        Stock = (int)GetDecimal(root, "stock", 0),
                        Category = GetString(root, "category", "general"),
                        Brand = GetString(root, "brand", "Generic"),
                        ThumbnailUrl = GetString(root, "thumbnailUrl", "https://placehold.co/400x400?text=Product")
                    };
                    var created = await _productService.CreateAsync(userId, dto);
                    return (JsonSerializer.Serialize(created), $"Created product '{created.Title}' (id {created.Id}).");
                }

                case "update_product":
                {
                    if (!isAdmin) return DenyForRole("update_product", "shopper");
                    var existing = await FindProductAsync(root);
                    if (existing is null)
                        return (JsonSerializer.Serialize(new { error = "Product not found." }), null);

                    var dto = new UpdateProductDto
                    {
                        Title = GetString(root, "title", existing.Title),
                        Description = GetString(root, "description", existing.Description),
                        Price = GetDecimal(root, "price", (double)existing.Price),
                        DiscountPercentage = GetDecimal(root, "discountPercentage", (double)existing.DiscountPercentage),
                        Stock = root.TryGetProperty("stock", out _) ? (int)GetDecimal(root, "stock") : existing.Stock,
                        Category = GetString(root, "category", existing.Category),
                        Brand = GetString(root, "brand", existing.Brand),
                        ThumbnailUrl = GetString(root, "thumbnailUrl", existing.ThumbnailUrl)
                    };
                    var updated = await _productService.UpdateAsync(existing.Id, dto);
                    return (JsonSerializer.Serialize(updated), $"Updated product '{updated.Title}' (id {updated.Id}).");
                }

                case "delete_product":
                {
                    if (!isAdmin) return DenyForRole("delete_product", "shopper");
                    var existing = await FindProductAsync(root);
                    if (existing is null)
                        return (JsonSerializer.Serialize(new { error = "Product not found." }), null);

                    await _productService.DeleteAsync(existing.Id);
                    return (JsonSerializer.Serialize(new { deleted = true, existing.Title }), $"Deleted product '{existing.Title}' (id {existing.Id}).");
                }

                case "list_users":
                {
                    if (!isAdmin) return DenyForRole("list_users", "shopper");
                    var users = await _userService.GetAllAsync();
                    return (JsonSerializer.Serialize(users.Take(20)), $"Looked up users (found {users.Count}).");
                }

                case "list_orders":
                {
                    if (!isAdmin) return DenyForRole("list_orders", "shopper");
                    var orders = await _orderService.GetAllAsync();
                    return (JsonSerializer.Serialize(orders.Take(20)), $"Looked up all orders (found {orders.Count}).");
                }

                case "update_order_status":
                {
                    if (!isAdmin) return DenyForRole("update_order_status", "shopper");
                    if (!root.TryGetProperty("orderId", out var idProp) || !Guid.TryParse(idProp.GetString(), out var orderId))
                        return (JsonSerializer.Serialize(new { error = "A valid orderId is required." }), null);

                    var dto = new UpdateOrderStatusDto
                    {
                        Status = GetString(root, "status", "Pending"),
                        CustomStatusLabel = root.TryGetProperty("customStatusLabel", out var lbl) ? lbl.GetString() : null
                    };
                    var updated = await _orderService.UpdateStatusAsync(orderId, dto);
                    return (JsonSerializer.Serialize(updated), $"Order {updated.Id} status set to '{updated.Status}'.");
                }

                default:
                    return (JsonSerializer.Serialize(new { error = $"Unknown tool '{toolCall.Name}'." }), null);
            }
        }
        catch (Exception ex)
        {
            return (JsonSerializer.Serialize(new { error = ex.Message }), null);
        }
    }

    private static (string, string?) DenyForRole(string tool, string actualRole) =>
        (JsonSerializer.Serialize(new { error = $"'{tool}' is not available to a {actualRole} account." }), null);

    private async Task<ProductDto?> FindProductAsync(JsonElement root)
    {
        if (root.TryGetProperty("id", out var idProp) && Guid.TryParse(idProp.GetString(), out var id))
        {
            try { return await _productService.GetByIdAsync(id); }
            catch { return null; }
        }

        if (root.TryGetProperty("title", out var titleProp))
        {
            var title = titleProp.GetString() ?? string.Empty;
            var matches = await _productService.GetAllAsync(title, null);
            return matches.FirstOrDefault();
        }

        return null;
    }

    private static string GetString(JsonElement root, string property, string fallback = "")
        => root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? fallback
            : fallback;

    private static decimal GetDecimal(JsonElement root, string property, double fallback = 0)
        => root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetDecimal()
            : (decimal)fallback;

    private static readonly LlmToolDefinition ListProductsTool = new()
    {
        Name = "list_products",
        Description = "Search/list admin-managed products, optionally filtered by text query and/or category.",
        InputSchema = new
        {
            type = "object",
            properties = new
            {
                query = new { type = "string", description = "Text to search product titles for" },
                category = new { type = "string", description = "Category to filter by" }
            },
            required = Array.Empty<string>()
        }
    };

    private static List<LlmToolDefinition> BuildShopperTools() => new()
    {
        ListProductsTool,
        new LlmToolDefinition
        {
            Name = "list_my_orders",
            Description = "List the current shopper's own past orders (read-only).",
            InputSchema = new { type = "object", properties = new { }, required = Array.Empty<string>() }
        },
        new LlmToolDefinition
        {
            Name = "cancel_my_order",
            Description = "Cancel one of the current shopper's own orders, if it's still cancellable (not already Delivered/Cancelled).",
            InputSchema = new
            {
                type = "object",
                properties = new { orderId = new { type = "string", description = "The order's GUID" } },
                required = new[] { "orderId" }
            }
        }
    };

    private static List<LlmToolDefinition> BuildAdminTools() => new()
    {
        new LlmToolDefinition
        {
            Name = "create_product",
            Description = "Create a new product in the catalog.",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    title = new { type = "string", description = "Product name" },
                    description = new { type = "string", description = "Product description" },
                    price = new { type = "number", description = "Price in USD" },
                    discountPercentage = new { type = "number", description = "Discount percent, 0-100" },
                    stock = new { type = "number", description = "Units in stock" },
                    category = new { type = "string", description = "Category name, e.g. electronics" },
                    brand = new { type = "string", description = "Brand name" },
                    thumbnailUrl = new { type = "string", description = "Image URL" }
                },
                required = new[] { "title", "price" }
            }
        },
        new LlmToolDefinition
        {
            Name = "update_product",
            Description = "Update an existing product. Provide id if known, otherwise title to look it up.",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    id = new { type = "string", description = "Product GUID, if known" },
                    title = new { type = "string", description = "Product name to find and/or set" },
                    description = new { type = "string" },
                    price = new { type = "number" },
                    discountPercentage = new { type = "number" },
                    stock = new { type = "number" },
                    category = new { type = "string" },
                    brand = new { type = "string" },
                    thumbnailUrl = new { type = "string" }
                },
                required = Array.Empty<string>()
            }
        },
        new LlmToolDefinition
        {
            Name = "delete_product",
            Description = "Delete a product by id or by title.",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    id = new { type = "string", description = "Product GUID, if known" },
                    title = new { type = "string", description = "Product name to find and delete" }
                },
                required = Array.Empty<string>()
            }
        },
        ListProductsTool,
        new LlmToolDefinition
        {
            Name = "list_users",
            Description = "List all registered users (read-only).",
            InputSchema = new { type = "object", properties = new { }, required = Array.Empty<string>() }
        },
        new LlmToolDefinition
        {
            Name = "list_orders",
            Description = "List all orders across all users, including who placed each one (read-only).",
            InputSchema = new { type = "object", properties = new { }, required = Array.Empty<string>() }
        },
        new LlmToolDefinition
        {
            Name = "update_order_status",
            Description = "Change an order's status (Pending/Paid/Shipped/Delivered/Cancelled), optionally with a custom message shown to the shopper.",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    orderId = new { type = "string", description = "The order's GUID" },
                    status = new { type = "string", description = "Pending, Paid, Shipped, Delivered, or Cancelled" },
                    customStatusLabel = new { type = "string", description = "Optional custom message shown to the shopper instead of the raw status" }
                },
                required = new[] { "orderId", "status" }
            }
        }
    };
}
