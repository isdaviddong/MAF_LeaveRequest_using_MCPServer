using Microsoft.Agents.AI;
using ModelContextProtocol.Client;
using OpenAI;
using OpenAI.Responses;

#pragma warning disable OPENAI001 

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.InputEncoding = System.Text.Encoding.UTF8;

// 從環境變數讀取 OpenAI API 金鑰
// PS:> $env:OPENAI_API_KEY = "你的金鑰"
var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    ?? throw new InvalidOperationException("請設置 OPENAI_API_KEY 環境變數。");

var mcpServerUrl = Environment.GetEnvironmentVariable("MCP_SERVER_URL")
    ?? "http://localhost:5050/mcp";
if (!Uri.TryCreate(mcpServerUrl, UriKind.Absolute, out var mcpEndpoint)
    || (mcpEndpoint.Scheme != Uri.UriSchemeHttps && mcpEndpoint.Scheme != Uri.UriSchemeHttp))
{
    throw new InvalidOperationException("請設置 MCP_SERVER_URL 為有效的 HTTP 或 HTTPS MCP 端點。");
}

await using var transport = new HttpClientTransport(new HttpClientTransportOptions
{
    Endpoint = mcpEndpoint,
    TransportMode = HttpTransportMode.StreamableHttp
});
using var startupTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
await using var mcpClient = await McpClient.CreateAsync(
    transport, cancellationToken: startupTimeout.Token);
var tools = await mcpClient.ListToolsAsync(cancellationToken: startupTimeout.Token);
Console.WriteLine($"MCP 已連線，載入 {tools.Count} 個工具。");

// 1) 建立一個 AI Agent，指定使用的模型和角色設定
var agent = new OpenAIClient(apiKey)
    .GetResponsesClient()
    .AsAIAgent(
        model: "gpt-4.1",
        name: "HelloBot",
        instructions: """
            你是一個友善且樂於助人的聊天機器人，叫做 HelloBot，也能協助員工請假。請用正體中文回答問題，並且保持對話的連貫性。
            查詢剩餘請假天數時，先取得員工姓名，再呼叫 GetLeaveRecordAmount。
            送出請假前，必須先取得請假者姓名、起始日期、整數請假天數、事由與代理人；資料不完整時逐項詢問，完整後才呼叫 LeaveRequest。
            需要判斷相對日期（例如明天）時，先呼叫 GetCurrentDate，並將請假起始日期轉成 yyyy-MM-dd。
            需要外部資料或操作時，請使用可用的 MCP 工具，並依照工具實際回傳的結果回答，不可捏造成功結果。
            """,
        tools: [.. tools]);

// 2) 建立一個對話 session，這樣才能保持多輪對話的上下文
var session = await agent.CreateSessionAsync();

Console.WriteLine("HelloBot：你好！輸入 /exit 離開，/reset 重新開始一段新對話。");

// 3) 進入對話迴圈
while (true)
{
    Console.Write("\n你：");
    var input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input))
        continue;

    if (input.Equals("/exit", StringComparison.OrdinalIgnoreCase))
        break;

    if (input.Equals("/reset", StringComparison.OrdinalIgnoreCase))
    {
        session = await agent.CreateSessionAsync();
        Console.WriteLine("HelloBot：已重置對話，我們重新開始吧！");
        continue;
    }

    try
    {
        // 4) 每次都把同一個 session 帶入，才能多輪對談
        var reply = await agent.RunAsync(input, session: session);
        Console.WriteLine($"\nHelloBot：{reply}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\n[錯誤] {ex.Message}");
    }
}

Console.WriteLine("\nHelloBot：掰掰！");