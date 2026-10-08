using System.Text;
using System.Text.Json;
using ChatClient;
using ChatProtocol;

// Client console demo: dùng NetworkClient (WebSocket + Envelope) của đề tài.
// Cách chạy:  dotnet run --project ChatClientConsole -- [host] [port]

var host = args.Length > 0 ? args[0] : "localhost";
var port = args.Length > 1 && int.TryParse(args[1], out var parsedPort) ? parsedPort : 5050;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

Console.Write("Tên của bạn: ");
var name = Console.ReadLine()?.Trim();
if (string.IsNullOrEmpty(name)) name = "guest" + Random.Shared.Next(100, 999);

var client = new NetworkClient();
var closed = new ManualResetEventSlim(false);
var consoleLock = new object();

void Print(string text, ConsoleColor color = ConsoleColor.Gray)
{
    lock (consoleLock)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ResetColor();
    }
}

async Task SendLoggedAsync(string type, object data)
{
    // In JSON thật được gửi đi để người xem thấy Envelope { type, data }.
    var json = JsonSerializer.Serialize(new { type, data }, FrameCodec.Options);
    Print($"--> {json}", ConsoleColor.DarkCyan);
    await client.SendAsync(type, data);
}

client.MessageReceived += env =>
{
    Print($"<-- {{\"type\":\"{env.Type}\",\"data\":{env.Data.GetRawText()}}}", ConsoleColor.DarkGray);
    switch (env.Type)
    {
        case "auth":
            Print($"[Đăng nhập] {env.As<AuthResponse>()?.Message}", ConsoleColor.Green);
            _ = SendLoggedAsync("join", new JoinRequest("General"));
            break;
        case "join":
            Print($"[Phòng] {env.As<JoinResponse>()?.Text}", ConsoleColor.Green);
            break;
        case "history":
            foreach (var m in env.As<HistoryResponse>()?.Messages ?? new List<ChatMessage>())
                Print($"  (lịch sử) {m.From}: {m.Text}");
            break;
        case "chat":
            var chat = env.As<ChatMessage>();
            if (chat != null) Print($"[{chat.Room}] {chat.From}: {chat.Text}", ConsoleColor.White);
            break;
        case "system":
            Print($"[Hệ thống] {env.As<SystemNotice>()?.Text}", ConsoleColor.Yellow);
            break;
        case "online-users":
            Print($"[Online] {string.Join(", ", env.As<OnlineUsersResponse>()?.Users ?? new List<string>())}", ConsoleColor.Yellow);
            break;
        case "room-list":
            Print($"[Phòng hiện có] {string.Join(" | ", (env.As<RoomListResponse>()?.Rooms ?? new List<RoomInfo>()).Select(r => r.ToString()))}", ConsoleColor.Yellow);
            break;
        case "error":
            Print($"[Lỗi] {env.As<ErrorResponse>()?.Message}", ConsoleColor.Red);
            break;
    }
};

client.Disconnected += reason =>
{
    Print($"[Ngắt kết nối] {reason}", ConsoleColor.Red);
    closed.Set();
};

try
{
    await client.ConnectAsync(host, port);
}
catch (Exception ex)
{
    Print($"Không kết nối được: {ex.Message}\nHãy chắc chắn ChatServer đang chạy.", ConsoleColor.Red);
    return;
}

Print($"Đã kết nối ws://{host}:{port}/ws. Lệnh: /join <phòng>, /create <phòng>, /quit. Gõ chữ bất kỳ để chat.", ConsoleColor.Green);
await SendLoggedAsync("login", new AuthRequest(name, "demo"));

while (!closed.IsSet)
{
    var line = Console.ReadLine();
    if (line == null) break;
    line = line.Trim();
    if (line.Length == 0) continue;

    if (line == "/quit") break;
    if (line.StartsWith("/join ")) await SendLoggedAsync("join", new JoinRequest(line[6..].Trim()));
    else if (line.StartsWith("/create ")) await SendLoggedAsync("create-room", new CreateRoomRequest(line[8..].Trim()));
    else await SendLoggedAsync("chat", new ChatRequest(line));
}

client.Close();
