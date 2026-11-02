# msagent

一個使用 [Microsoft Agent Framework](https://github.com/microsoft/agent-framework) 與 [OpenAI API](https://platform.openai.com/) 建立的多輪對話命令列聊天機器人。

## 專案結構

```text
HOL_MSAgentFrameworkRC\
  msagent.sln
  README.md
  msagent\
    msagent.csproj
    Program.cs
  LeaveMcpServer\
    LeaveMcpServer.csproj
    Program.cs
```

兩個專案位於同一層級的獨立資料夾，透過 HTTP MCP 通訊，不使用專案參考。
以下指令皆從方案根目錄執行。

## 功能

- 透過 OpenAI `gpt-4.1` 模型進行對話
- 支援多輪對話（保持上下文連貫性）
- 透過 Streamable HTTP 連線遠端 MCP server，讓模型視需要呼叫其工具
- 方案內含 `LeaveMcpServer` 專案，提供查詢請假餘額、模擬請假與取得台灣時間的工具
- 輸入 `/reset` 可重置對話，開始新的一輪
- 輸入 `/exit` 可離開程式

## 環境需求

- [.NET 10.0](https://dotnet.microsoft.com/download) 或更新版本
- 有效的 [OpenAI API 金鑰](https://platform.openai.com/api-keys)
- 可用的 Streamable HTTP MCP 端點（目前不使用驗證）

## 快速開始

### 1. 設定 API 金鑰

先至 [OpenAI API Keys](https://platform.openai.com/api-keys) 建立有效的 API key。
此專案使用 OpenAI API key，不是 Azure OpenAI 金鑰；ChatGPT 訂閱也不等同於 OpenAI API 額度。
只有 HelloBot 需要此金鑰，MCP server 不需要。

**方式一：僅限目前 PowerShell 工作階段**

```powershell
$env:OPENAI_API_KEY = "你的 OpenAI API key"
dotnet run --project .\msagent\msagent.csproj
```

必須在設定金鑰的同一個終端機啟動 HelloBot（並先在另一個終端機啟動 MCP server）。
其他已開啟的終端機、VS Code 或測試程序不會自動取得此設定。

**方式二：永久設定至 Windows 使用者環境變數**

```powershell
[System.Environment]::SetEnvironmentVariable("OPENAI_API_KEY", "你的 OpenAI API key", "User")
```

設定後請完全關閉並重新啟動 VS Code，再開啟新的終端機。
若要讓目前 PowerShell 立即使用已儲存的使用者設定，可執行：

```powershell
$env:OPENAI_API_KEY = [System.Environment]::GetEnvironmentVariable("OPENAI_API_KEY", "User")
```

**確認目前終端機是否已設定（不顯示金鑰內容）**

```powershell
if ([string]::IsNullOrWhiteSpace($env:OPENAI_API_KEY)) {
    Write-Host "尚未設定 OPENAI_API_KEY"
} else {
    Write-Host "OPENAI_API_KEY 已設定"
}
```

此檢查只確認變數有值，不代表金鑰通過 OpenAI 驗證。若呼叫時出現
`HTTP 401` / `invalid_api_key`，請確認金鑰正確、未撤銷，且目前執行程序沒有沿用舊金鑰；
更新後重新啟動 HelloBot。若出現額度相關錯誤，請至 OpenAI 平台檢查 API 計費與用量限制。

> 請勿將真實金鑰寫入程式碼、README、版本控制或貼到對話中。
> 上述指令中的金鑰可能留在 PowerShell 歷程紀錄；使用者環境變數也不是加密的秘密儲存庫，
> 僅適合本機開發示範。若金鑰外洩，請立即撤銷並重新產生。

### 2. 啟動請假 MCP server

在第一個終端機執行：

```powershell
dotnet run --project .\LeaveMcpServer\LeaveMcpServer.csproj
```

Server 預設監聽 `http://localhost:5050`，MCP 端點為 `http://localhost:5050/mcp`。
這是 Streamable HTTP server，不是 Stdio；必須先啟動 server 再啟動 HelloBot。
目前未設定驗證，僅供本機示範，請勿直接公開至網際網路。

| 工具 | 功能 |
|------|------|
| `GetLeaveRecordAmount` | David 回傳 5 天、Eric 回傳 8 天，其他員工回傳 3 天 |
| `LeaveRequest` | 模擬請假，姓名、日期、天數、事由與代理人皆必填；天數須為正整數，日期格式為 `yyyy-MM-dd` |
| `GetCurrentDate` | 回傳台灣時間（UTC+8），格式為 `yyyy-MM-dd HH:mm:ss` |

請假結果不會寫入資料庫，餘額也不會因請假而扣減。

### 3. 啟動 HelloBot

在第二個終端機設定上述 `OPENAI_API_KEY`，再執行：

```powershell
dotnet run --project .\msagent\msagent.csproj
```

HelloBot 預設連到 `http://localhost:5050/mcp`。若要覆寫成其他端點：

```powershell
$env:MCP_SERVER_URL = "https://你的實際伺服器/mcp"
```

若先前設定過 `MCP_SERVER_URL`，可用 `Remove-Item Env:MCP_SERVER_URL -ErrorAction SilentlyContinue` 清除，恢復本機預設值。
遠端工具的描述及相關對話參數會送往 OpenAI；請勿使用會洩漏敏感資料的工具。
正式遠端連線建議使用 HTTPS。

啟動時會連線並列出 MCP 工具，再註冊至 Agent。模型會依對話需求決定是否呼叫工具，
不會每次對話都呼叫。連線或工具探索失敗會顯示例外並停止啟動，不會假裝連線成功；
連線及工具探索共用 30 秒逾時。離開程式時會釋放連線，`/reset` 只重置對話。

### 4. 建置整個方案

```powershell
dotnet build .\msagent.sln
```

## 使用方式

程式啟動後即可直接輸入文字與機器人對話：

```
HelloBot：你好！輸入 /exit 離開，/reset 重新開始一段新對話。

你：你好，請介紹一下你自己
HelloBot：你好！我是 HelloBot，一個友善且樂於助人的聊天機器人...

你：David 還有幾天假？
HelloBot：David 還有 5 天假。

你：/reset
HelloBot：已重置對話，我們重新開始吧！

你：/exit
HelloBot：掰掰！
```

| 指令 | 說明 |
|------|------|
| `/reset` | 重置對話，清除所有上下文 |
| `/exit` | 離開程式 |

## 套件相依

| 套件 | 版本 |
|------|------|
| `Microsoft.Agents.AI.OpenAI` | 1.27.0 |
| `ModelContextProtocol.Core` | 2.1.0 |
| `ModelContextProtocol.AspNetCore`（Server） | 2.1.0 |

此版本使用 `GetResponsesClient()` 建立 Responses client，並在 `AsAIAgent(model: "gpt-4.1", ...)` 指定模型。
