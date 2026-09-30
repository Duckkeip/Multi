"""
client.py - TCP Chat Client (minh họa Transport Layer)
--------------------------------------------------------
Kết nối tới server.py qua TCP Socket. Chạy nhiều client cùng lúc để
mô phỏng nhiều tiến trình ứng dụng dùng chung hạ tầng mạng nhưng
được tầng Transport phân biệt qua (IP, Port) của từng kết nối.

Cách chạy:
    python client.py               (nếu server chạy trên cùng máy)
    python client.py 192.168.1.10  (nếu server chạy trên máy khác trong LAN)
"""

import socket
import sys
import threading

PORT = 5555


def receive_loop(sock: socket.socket):
    """Luồng riêng để liên tục nhận dữ liệu từ server (recv là lệnh block)."""
    while True:
        try:
            data = sock.recv(1024)
        except OSError:
            break
        if not data:
            print("\n[!] Mất kết nối tới server.")
            break
        print(data.decode("utf-8"), end="")


def main():
    server_ip = sys.argv[1] if len(sys.argv) > 1 else "127.0.0.1"

    # Tạo socket TCP và chủ động connect() -> gửi gói SYN đầu tiên,
    # bắt đầu bắt tay 3 bước ở tầng Transport
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.connect((server_ip, PORT))
    print(f"[CLIENT] Đã kết nối tới {server_ip}:{PORT} (TCP)")

    threading.Thread(target=receive_loop, args=(sock,), daemon=True).start()

    try:
        while True:
            msg = input()
            if msg.strip().lower() in ("/exit", "/quit"):
                break
            sock.sendall(msg.encode("utf-8"))
    except (KeyboardInterrupt, EOFError):
        pass
    finally:
        sock.close()


if __name__ == "__main__":
    main()
