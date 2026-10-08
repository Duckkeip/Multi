# Demo Multi Chat Room - WebSocket trên TCP

Bản demo rút gọn, chạy được mà KHÔNG cần MongoDB / email / OTP (mọi thứ nằm trong RAM).
Dùng nguyên các file của đề tài: `Protocol.cs`, `ClientSession.cs`, `RoomManager.cs`, `NetworkClient.cs`.
`ChatServer/Program.cs` là bản rút gọn của `Program.cs` gốc (chỉ giữ login, join, create-room, chat, typing).

## Yêu cầu
- .NET 8 SDK (https://dotnet.microsoft.com/download), kiểm tra bằng `dotnet --version`.

## Cách chạy
Mở các terminal tại thư mục `ChatDemo`:

1. Server:
   dotnet run --project ChatServer
   -> in ra  ws://localhost:5050/ws  và  http://localhost:5050/

2. Client A (trình duyệt): mở http://localhost:5050/ , bấm "Kết nối & đăng nhập".
3. Client B (trình duyệt): mở thêm tab/cửa sổ thứ hai cùng địa chỉ, đặt tên khác.
4. (Tùy chọn) Client console dùng NetworkClient:
   dotnet run --project ChatClientConsole
   Lệnh: /join <phòng>, /create <phòng>, /quit; gõ chữ bất kỳ để chat.

Đổi cổng: `dotnet run --project ChatServer -- 6000` (client console: `-- localhost 6000`).

## Kịch bản demo gợi ý (khớp các slide)
1. Slide 4-5: mở server, chỉ vào dòng "WebSocket server đang chạy: ws://localhost:5050/ws".
2. Slide 10: tab A đăng nhập, nhìn khung "Envelope JSON" bên phải:
   --> {"type":"login","data":{...}}   <-- {"type":"auth",...}   rồi tự động join General.
3. Tab B đăng nhập: tab A nhận thông báo "B đã vào phòng" (type system) và danh sách online-users.
4. Gõ chat ở tab A: cả A và B nhận {"type":"chat",...} (broadcast cả phòng, slide 8).
5. Tab B đổi sang phòng "Tech": A thấy "B đã rời phòng"; chat ở Tech không tới A.
6. Đóng tab B: A thấy "B đã ngắt kết nối" (HandleDisconnectAsync, slide 6).
7. Gửi thử type lạ (console trình duyệt: ws.send('{"type":"abc","data":{}}')) -> server trả "error".
8. Chat khi chưa đăng nhập -> server trả "error" (kiểm tra IsAuthenticated).

## Khác với bản đầy đủ
- Đăng nhập: nhập tên bất kỳ (mật khẩu bỏ qua), không lưu tài khoản; trùng tên đang online sẽ bị từ chối.
- Lịch sử chat chỉ giữ trong RAM (50 tin gần nhất mỗi phòng), mất khi tắt server.
- Chưa có: đăng ký OTP, DM, bạn bè, voice, upload file, 2FA, AI. Muốn chạy bản đầy đủ cần
  MONGODB_URI trong .env cùng các class còn thiếu (MongoUserStore, EmailService...).
- Client WinForms gốc không nằm trong gói này.
