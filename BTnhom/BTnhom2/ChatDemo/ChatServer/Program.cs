using System.Collections.Concurrent;
using ChatProtocol;

namespace ChatServer;

/// <summary>
/// Server demo rút gọn: giữ nguyên luồng WebSocket + Envelope + RoomManager của đề tài,
/// nhưng lưu mọi thứ trong RAM (không cần MongoDB, email, OTP).
/// </summary>
public static class Program
{
    private static readonly string[] DefaultRooms = { "General", "Random", "Tech" };
    private static readonly RoomManager Rooms = new(DefaultRooms);
    private static readonly List<ClientSession> AllClients = new();
    private static readonly object AllClientsLock = new();
    private static readonly ConcurrentDictionary<string, List<ChatMessage>> History = new();

    public static async Task Main(string[] args)
    {
        var port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 5050;

        var builder = WebApplication.CreateBuilder(Array.Empty<string>());
        builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
        var app = builder.Build();

        app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(30) });
        app.UseDefaultFiles();   // phục vụ wwwroot/index.html (trang test trên trình duyệt)
        app.UseStaticFiles();
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

        app.Map("/ws", async context =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("WebSocket connection required.");
                return;
            }

            var socket = await context.WebSockets.AcceptWebSocketAsync();
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var session = new ClientSession(socket, ip);
            lock (AllClientsLock) AllClients.Add(session);
            Console.WriteLine($"[WS] Kết nối mới từ {ip}");
            await HandleClientAsync(session);
        });

        Console.WriteLine($"[ChatServer] WebSocket server đang chạy: ws://localhost:{port}/ws");
        Console.WriteLine($"[ChatServer] Trang test trình duyệt:      http://localhost:{port}/");
        await app.RunAsync();
    }

    // Vòng lặp nhận: mỗi WebSocket message = 1 Envelope JSON.
    private static async Task HandleClientAsync(ClientSession session)
    {
        try
        {
            while (true)
            {
                Envelope? envelope;
                try
                {
                    envelope = await WebSocketEnvelopeCodec.ReceiveAsync(session.Socket);
                }
                catch (InvalidDataException ex)
                {
                    Console.WriteLine($"[!] Frame không hợp lệ: {ex.Message}");
                    break;
                }
                if (envelope == null) break; // client đóng kết nối

                Console.WriteLine($"[<-] {(session.Username.Length > 0 ? session.Username : session.IpAddress)}: type={envelope.Type}");
                await DispatchAsync(session, envelope);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WS] Kết nối đóng: {ex.Message}");
        }
        finally
        {
            await HandleDisconnectAsync(session);
        }
    }

    // Điều phối theo envelope.Type.
    private static async Task DispatchAsync(ClientSession session, Envelope envelope)
    {
        switch (envelope.Type)
        {
            case "login":       await HandleLoginAsync(session, envelope.As<AuthRequest>()); break;
            case "join":        await HandleJoinAsync(session, envelope.As<JoinRequest>()); break;
            case "create-room": await HandleCreateRoomAsync(session, envelope.As<CreateRoomRequest>()); break;
            case "chat":        await HandleChatAsync(session, envelope.As<ChatRequest>()); break;
            case "typing":      await HandleTypingAsync(session, envelope.As<TypingRequest>()); break;
            default:
                await session.SendAsync("error", new ErrorResponse("Loại message không hỗ trợ."));
                break;
        }
    }

    private static async Task HandleLoginAsync(ClientSession session, AuthRequest? req)
    {
        var name = (req?.Username ?? "").Trim();
        if (name.Length == 0 || name.Length > 32 || string.IsNullOrEmpty(req?.Password))
        {
            await session.SendAsync("error", new ErrorResponse("Cần username (1-32 ký tự) và mật khẩu (demo: nhập gì cũng được)."));
            return;
        }

        bool taken;
        lock (AllClientsLock)
            taken = AllClients.Any(c => c != session && c.IsAuthenticated &&
                                        string.Equals(c.Username, name, StringComparison.OrdinalIgnoreCase));
        if (taken)
        {
            await session.SendAsync("error", new ErrorResponse($"'{name}' đang được dùng, hãy chọn tên khác."));
            return;
        }

        session.Username = name;
        session.IsAuthenticated = true;
        await session.SendAsync("auth", new AuthResponse(name, "Đăng nhập thành công (bản demo, không cần MongoDB)."));
        await session.SendAsync("room-list", new RoomListResponse(Rooms.GetRoomList()));
    }

    private static async Task HandleJoinAsync(ClientSession session, JoinRequest? req)
    {
        if (!session.IsAuthenticated)
        {
            await session.SendAsync("error", new ErrorResponse("Bạn cần đăng nhập trước."));
            return;
        }

        var room = (req?.Room ?? "").Trim();
        if (!ChatLimits.IsValidRoomName(room))
        {
            await session.SendAsync("error", new ErrorResponse($"Tên phòng phải có từ 1 đến {ChatLimits.MaxRoomNameLength} ký tự."));
            return;
        }

        var previousRoom = session.CurrentRoom;
        var username = session.Username;

        Rooms.CreateRoomIfMissing(room);
        Rooms.Join(session, room);

        if (previousRoom != null && previousRoom != room)
        {
            await Rooms.BroadcastAsync(previousRoom, "system",
                new SystemNotice(previousRoom, $"{username} đã rời phòng.", DateTimeOffset.Now));
            await Rooms.BroadcastAsync(previousRoom, "online-users",
                new OnlineUsersResponse(previousRoom, Rooms.GetUsernames(previousRoom)));
        }

        await Rooms.BroadcastAsync(room, "system",
            new SystemNotice(room, $"{username} đã vào phòng.", DateTimeOffset.Now), except: session);

        await session.SendAsync("join", new JoinResponse(room, $"Đã vào phòng {room}."));

        List<ChatMessage> recent;
        if (History.TryGetValue(room, out var list))
            lock (list) recent = list.TakeLast(50).ToList();
        else
            recent = new List<ChatMessage>();
        await session.SendAsync("history", new HistoryResponse(room, recent));

        await Rooms.BroadcastAsync(room, "online-users",
            new OnlineUsersResponse(room, Rooms.GetUsernames(room)));
        await BroadcastRoomListToAllAsync();
    }

    private static async Task HandleCreateRoomAsync(ClientSession session, CreateRoomRequest? req)
    {
        if (!session.IsAuthenticated) return;

        var room = (req?.Room ?? "").Trim();
        if (!ChatLimits.IsValidRoomName(room))
        {
            await session.SendAsync("error", new ErrorResponse("Tên phòng không hợp lệ."));
            return;
        }
        if (Rooms.RoomExists(room))
        {
            await session.SendAsync("error", new ErrorResponse($"Phòng '{room}' đã tồn tại."));
            return;
        }
        Rooms.CreateRoomIfMissing(room);
        await BroadcastRoomListToAllAsync();
    }

    private static async Task HandleChatAsync(ClientSession session, ChatRequest? req)
    {
        if (!session.IsAuthenticated || session.CurrentRoom == null)
        {
            await session.SendAsync("error", new ErrorResponse("Hãy đăng nhập và vào một phòng trước khi chat."));
            return;
        }

        var text = (req?.Text ?? "").Trim();
        if (!ChatLimits.IsValidChatText(text))
        {
            await session.SendAsync("error", new ErrorResponse($"Tin nhắn phải có từ 1 đến {ChatLimits.MaxChatTextLength} ký tự."));
            return;
        }

        var room = session.CurrentRoom;
        var message = new ChatMessage(Guid.NewGuid().ToString(), room, session.Username, text,
            DateTimeOffset.Now, DisplayName: session.Username);

        var list = History.GetOrAdd(room, _ => new List<ChatMessage>());
        lock (list)
        {
            list.Add(message);
            if (list.Count > 200) list.RemoveAt(0);
        }

        // Broadcast tới tất cả thành viên trong phòng (kể cả người gửi).
        await Rooms.BroadcastAsync(room, "chat", message);
    }

    private static async Task HandleTypingAsync(ClientSession session, TypingRequest? req)
    {
        if (!session.IsAuthenticated || session.CurrentRoom == null) return;
        await Rooms.BroadcastAsync(session.CurrentRoom, "typing",
            new TypingNotice(session.CurrentRoom, session.Username, req?.IsTyping ?? false), except: session);
    }

    private static async Task HandleDisconnectAsync(ClientSession session)
    {
        var room = session.CurrentRoom;
        Rooms.Leave(session);
        lock (AllClientsLock) AllClients.Remove(session);
        session.Close();

        if (room != null && session.Username.Length > 0)
        {
            await Rooms.BroadcastAsync(room, "system",
                new SystemNotice(room, $"{session.Username} đã ngắt kết nối.", DateTimeOffset.Now));
            await Rooms.BroadcastAsync(room, "online-users",
                new OnlineUsersResponse(room, Rooms.GetUsernames(room)));
        }
        Console.WriteLine($"[-] {(session.Username.Length > 0 ? session.Username : session.IpAddress)} đã ngắt kết nối.");
        await BroadcastRoomListToAllAsync();
    }

    private static async Task BroadcastRoomListToAllAsync()
    {
        List<ClientSession> targets;
        lock (AllClientsLock) targets = AllClients.Where(c => c.IsAuthenticated).ToList();
        var payload = new RoomListResponse(Rooms.GetRoomList());
        await Task.WhenAll(targets.Select(c => c.SendAsync("room-list", payload)));
    }
}
