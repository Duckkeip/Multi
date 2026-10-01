# Chat TCP WinForms

Ứng dụng chat nhiều phòng viết bằng C# WinForms, giao tiếp TCP với server riêng và
lưu dữ liệu bằng MongoDB.

## Thành phần

```text
ChatTcpWinForms.sln
├── ChatProtocol/   # Class Library net10.0, hợp đồng message dùng chung
├── ChatServer/     # ASP.NET Core net10.0, WebSocket server
└── ChatClient/     # WinForms app net10.0-windows
```

Client và server cùng tham chiếu `ChatProtocol`, vì vậy payload được định nghĩa một
lần trong `ChatProtocol/Protocol.cs`.

## Tính năng đã có

- Đăng ký tài khoản bằng email và OTP.
- Đăng nhập, đăng xuất, quên mật khẩu và đổi mật khẩu bằng OTP email.
- Chat nhiều phòng thời gian thực; có sẵn `General`, `Random`, `Tech` và cho phép tạo phòng mới.
- Lưu và tải lại lịch sử 50 tin nhắn gần nhất của từng phòng.
- Danh sách người dùng online, trạng thái đang nhập và thông báo hệ thống khi vào/rời phòng.
- Nhắn tin riêng; tin nhắn gửi cho người đang offline được lưu và phát lại khi họ đăng nhập.
- Hồ sơ cá nhân: avatar, tên hiển thị, email, tiểu sử và trạng thái.
- Quyền riêng tư: cho phép nhắn riêng, hiển thị avatar, hiển thị trạng thái online và chặn người dùng.
- Gửi tệp tối đa 12 MB theo từng chunk; ảnh có thumbnail. Tệp được lưu bằng MongoDB GridFS.
- Giao diện dark/light/system, màu chủ đạo, cỡ chữ chat và các tùy chọn thông báo.
- Quản lý các phiên đăng nhập, thu hồi một phiên hoặc đăng xuất các thiết bị khác.
- Xác thực hai bước TOTP: tạo QR, mã bí mật, mã dự phòng, bật/tắt và kiểm tra trạng thái.
- Lịch sử đăng nhập.
- Gọi AI bằng lệnh `/ai câu hỏi` nếu cấu hình `AI_SERVICE_URL`.

## Giao thức WebSocket

Client kết nối tới endpoint `/ws` bằng WebSocket. Trên Render, client dùng `wss://`;
ở local, client dùng `ws://localhost:5050/ws`. Mỗi WebSocket message chứa một JSON
`Envelope { type, data }`, nên không cần length-prefix TCP. Giới hạn message là 6 MB;
tệp được chia thành chunk 512 KB trước khi gửi. Endpoint `/health` trả HTTP 200 để Render
kiểm tra trạng thái service.

## Yêu cầu

- Windows.
- .NET 10 SDK.
- MongoDB nếu muốn dùng tài khoản, lịch sử chat, DM offline, file và tính năng bảo mật.
- Gmail SMTP App Password nếu muốn dùng OTP email.

## Cấu hình server

Server đọc file `.env` trong thư mục `ChatServer` hoặc biến môi trường hệ thống. Sao chép
`ChatServer/.env.example` thành `ChatServer/.env`, rồi thay các giá trị mẫu:

```env
MONGODB_URI=mongodb://localhost:27017/chatnet
PORT=5050
EMAIL_USER=your-email@gmail.com
EMAIL_APP_PASSWORD=your-gmail-app-password
JWT_SECRET=long-random-secret
AI_SERVICE_URL=https://your-ai-service.example
LIVEKIT_URL=wss://your-project.livekit.cloud
LIVEKIT_API_KEY=your-livekit-api-key
LIVEKIT_API_SECRET=your-livekit-api-secret
```

Các biến:

- `MONGODB_URI`: connection string MongoDB. Nếu URI không nêu tên database, server dùng `multiroom_chat`.
- `PORT`: cổng HTTP/WebSocket, mặc định `5050` khi chạy local; Render cấp giá trị runtime.
- `EMAIL_USER` và `EMAIL_APP_PASSWORD`: dùng để gửi OTP qua Gmail SMTP.
- `JWT_SECRET`: dùng để ký token OTP có thời hạn; không phải token đăng nhập client.
- `AI_SERVICE_URL`: URL service có endpoint `POST /generate`, nhận `prompt`, `room`, `username` và trả `{ "reply": "..." }`.
- `LIVEKIT_URL`: WebSocket URL của LiveKit Cloud hoặc LiveKit server.
- `LIVEKIT_API_KEY` và `LIVEKIT_API_SECRET`: credentials chỉ dùng trên server để ký token; không đặt chúng trong `index.html`.

Không commit `.env` hoặc mật khẩu thật lên Git.

## Chạy

1. Restore và build solution:

   ```powershell
   dotnet build .\ChatTcpWinForms.sln
   ```

2. Tạo và điền `ChatServer/.env`.

3. Chạy server trước:

   ```powershell
   dotnet run --project .\ChatServer\ChatServer.csproj
   ```

4. Chạy client:

   ```powershell
   dotnet run --project .\ChatClient\ChatClient.csproj
   ```

Client mặc định kết nối `ws://localhost:5050/ws`. Với Render, nhập hostname service
vào trường máy chủ và `443` vào trường cổng; client sẽ tự dùng `wss://<hostname>:443/ws`.
Đặt health check path của Render thành `/health`. Chạy server không có MongoDB vẫn mở
endpoint, nhưng các tính năng cần lưu trữ sẽ không hoạt động.

## Dữ liệu MongoDB

Server sử dụng các collection/document sau:

- `users`: tài khoản, mật khẩu PBKDF2, hồ sơ, cài đặt, phiên và lịch sử đăng nhập.
- `messages`: lịch sử tin nhắn phòng.
- `offline_direct_messages`: hàng đợi tin nhắn riêng offline.
- `chat_files.files` và `chat_files.chunks`: tệp gửi trong chat qua GridFS.

## Giới hạn hiện tại

- Phiên TCP là trạng thái kết nối hiện tại; JWT chỉ được dùng cho quy trình OTP.
- AI chỉ hoạt động khi service bên ngoài được cấu hình và đang phản hồi.
- Chưa có migration hoặc seed dữ liệu riêng; các collection/index được khởi tạo khi server kết nối MongoDB.
