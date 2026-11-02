using System.ComponentModel;
using System.Globalization;
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(builder.Configuration["urls"] ?? "http://localhost:5050");

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();
app.MapMcp("/mcp");
await app.RunAsync();

[McpServerToolType]
public static class LeaveRequestTool
{
    [McpServerTool(Name = nameof(GetLeaveRecordAmount)), Description("取得員工剩餘的請假天數")]
    public static int GetLeaveRecordAmount(
        [Description("要查詢請假天數的員工名稱")] string employeeName)
    {
        McpToolInvocationLogger.Log(nameof(GetLeaveRecordAmount));

        if (string.IsNullOrWhiteSpace(employeeName))
            throw new ArgumentException("員工名稱不可為空白。", nameof(employeeName));

        return employeeName.Trim().ToLowerInvariant() switch
        {
            "david" => 5,
            "eric" => 8,
            _ => 3
        };
    }

    [McpServerTool(Name = nameof(LeaveRequest)), Description("模擬進行請假，回傳結果；不會儲存假單")]
    public static string LeaveRequest(
        [Description("請假起始日期，格式 yyyy-MM-dd")] string 請假起始日期,
        [Description("請假天數，必須為正整數")] int 天數,
        [Description("請假事由")] string 請假事由,
        [Description("代理人")] string 代理人,
        [Description("請假者姓名")] string 請假者姓名)
    {
        McpToolInvocationLogger.Log(nameof(LeaveRequest));

        if (string.IsNullOrWhiteSpace(請假起始日期)
            || string.IsNullOrWhiteSpace(請假事由)
            || string.IsNullOrWhiteSpace(代理人)
            || string.IsNullOrWhiteSpace(請假者姓名))
        {
            return "請假失敗，請確認所有必填欄位已填寫。";
        }

        if (!DateOnly.TryParseExact(請假起始日期, "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            return "請假失敗，請假起始日期必須為有效的 yyyy-MM-dd 日期。";
        }

        if (天數 <= 0)
            return "請假失敗，請假天數必須為正整數。";

        return $"{請假者姓名} 請假 {天數}天，從 {請假起始日期} 開始，事由為 {請假事由}，代理人 {代理人}";
    }

    [McpServerTool(Name = nameof(GetCurrentDate)), Description("取得今天日期，時區為台灣（UTC+8）")]
    public static string GetCurrentDate()
    {
        McpToolInvocationLogger.Log(nameof(GetCurrentDate));

        return DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8))
            .ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }
}

public static class McpToolInvocationLogger
{
    private static readonly object ConsoleLock = new();

    public static void Log(string toolName)
    {
        lock (ConsoleLock)
        {
            var originalColor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[MCP Tool Called] {toolName}");
            }
            finally
            {
                Console.ForegroundColor = originalColor;
            }
        }
    }
}
