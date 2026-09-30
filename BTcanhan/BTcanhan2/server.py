"""
server.py - TCP Chat Server (minh họa Transport Layer)
--------------------------------------------------------
Mục đích: minh họa mối liên hệ giữa TẦNG TRANSPORT (OSI Layer 4)
và lập trình phần mềm thông qua Socket API.

Các khái niệm Transport Layer thể hiện trong code này:
  - Port (5555): cơ chế MULTIPLEXING - phân biệt ứng dụng trên cùng 1 máy
  - socket(AF_INET, SOCK_STREAM): chọn giao thức TCP (connection-oriented)
  - accept(): hoàn tất bắt tay 3 bước (3-way handshake: SYN, SYN-ACK, ACK)
    do hệ điều hành/TCP stack tự thực hiện, trước khi dòng code này return
  - send()/recv(): dữ liệu được TCP tự chia thành segment, đánh số thứ tự,
    gửi ACK và tự động truyền lại (retransmit) nếu mất gói - lập trình
    viên KHÔNG cần tự cài đặt các cơ chế này

Cách chạy:
    python server.py
Sau đó mở nhiều cửa sổ terminal, mỗi cửa sổ chạy 1 client.py để giả lập
nhiều client cùng kết nối (multi-client chat).

Gợi ý quay clip: mở Wireshark, lọc "tcp.port == 5555" trước khi chạy
client để bắt được gói SYN / SYN-ACK / ACK lúc bắt tay.
"""

import socket
import threading

HOST = "0.0.0.0"
PORT = 5555  # Port = định danh Transport-layer cho ứng dụng chat này

# Danh sách client đang kết nối: {conn: username}
clients = {}
clients_lock = threading.Lock()


def broadcast(message: str, exclude_conn=None):
    """Gửi message tới tất cả client đang kết nối (trừ exclude_conn)."""
    with clients_lock:
        for conn in list(clients.keys()):
            if conn is exclude_conn:
                continue
            try:
                conn.sendall(message.encode("utf-8"))
            except OSError:
                pass


def handle_client(conn: socket.socket, addr):
    """
    Mỗi client được xử lý trên 1 thread riêng.
    'conn' là một socket TCP RIÊNG cho kết nối này - đây chính là minh
    chứng rõ nhất cho khái niệm "end-to-end connection" của Transport
    Layer: server có thể nói chuyện độc lập với từng client dù dùng
    chung 1 địa chỉ IP và 1 port lắng nghe.
    """
    print(f"[+] Kết nối mới từ {addr}")
    try:
        conn.sendall("Nhập tên của bạn: ".encode("utf-8"))
        username = conn.recv(1024).decode("utf-8").strip()

        with clients_lock:
            clients[conn] = username or f"User_{addr[1]}"

        welcome = f"*** {clients[conn]} đã tham gia phòng chat ***\n"
        print(welcome.strip())
        broadcast(welcome, exclude_conn=conn)

        while True:
            data = conn.recv(1024)
            if not data:
                break  # client đóng kết nối (FIN từ tầng Transport)
            text = data.decode("utf-8").strip()
            if not text:
                continue
            print(f"[{clients[conn]}] {text}")
            broadcast(f"[{clients[conn]}] {text}\n", exclude_conn=conn)

    except ConnectionResetError:
        pass
    finally:
        with clients_lock:
            name = clients.pop(conn, "Ai đó")
        leave_msg = f"*** {name} đã rời phòng chat ***\n"
        print(leave_msg.strip())
        broadcast(leave_msg)
        conn.close()


def main():
    # AF_INET  -> địa chỉ IPv4 (tầng Network)
    # SOCK_STREAM -> chọn TCP (tầng Transport, connection-oriented)
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server_socket.bind((HOST, PORT))
    server_socket.listen()
    print(f"[SERVER] Đang lắng nghe tại {HOST}:{PORT} (TCP)")

    try:
        while True:
            # accept() chỉ trả về khi bắt tay 3 bước đã hoàn tất
            conn, addr = server_socket.accept()
            thread = threading.Thread(target=handle_client, args=(conn, addr), daemon=True)
            thread.start()
    except KeyboardInterrupt:
        print("\n[SERVER] Đang tắt server...")
    finally:
        server_socket.close()


if __name__ == "__main__":
    main()
