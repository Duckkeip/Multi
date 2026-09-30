"""
Gui email bang socket THO - khong dung smtplib.
Muc dich: chung minh hieu ro giao thuc SMTP o muc thap, tu go tung lenh:
EHLO -> STARTTLS -> AUTH LOGIN -> MAIL FROM -> RCPT TO -> DATA -> QUIT

Day la phan "an tuong" nhat khi thuyet trinh vi cho thay nhom hieu ban chat
giao thuc, khong chi goi ham co san.
"""

import socket
import ssl
import base64

SMTP_SERVER = "smtp.gmail.com"
SMTP_PORT = 587

SENDER_EMAIL = "your_email@gmail.com"
APP_PASSWORD = "xxxx xxxx xxxx xxxx"
RECEIVER_EMAIL = "receiver@example.com"


def recv(sock):
    """Nhan va in phan hoi tu server (moi lenh SMTP deu tra ve 1 dong ma so)."""
    response = sock.recv(1024).decode(errors="ignore")
    print("<<<", response.strip())
    return response


def send_cmd(sock, command):
    """Gui 1 lenh SMTP, ket thuc bang \\r\\n theo chuan giao thuc."""
    print(">>>", command.strip())
    sock.sendall((command + "\r\n").encode())


def raw_smtp_send():
    # Buoc 1: Ket noi TCP thuan toi server SMTP, port 587
    sock = socket.create_connection((SMTP_SERVER, SMTP_PORT), timeout=10)
    recv(sock)  # server gui banner chao (220 ...)

    # Buoc 2: EHLO - gioi thieu client, server tra ve danh sach tinh nang ho tro
    send_cmd(sock, f"EHLO {socket.gethostname()}")
    recv(sock)

    # Buoc 3: STARTTLS - yeu cau nang cap ket noi len ma hoa TLS
    send_cmd(sock, "STARTTLS")
    recv(sock)
    context = ssl.create_default_context()
    sock = context.wrap_socket(sock, server_hostname=SMTP_SERVER)  # tu day tro di ket noi da duoc ma hoa

    # Sau khi wrap TLS, phai EHLO lai lan nua theo chuan giao thuc
    send_cmd(sock, f"EHLO {socket.gethostname()}")
    recv(sock)

    # Buoc 4: AUTH LOGIN - xac thuc, username/password phai encode base64
    send_cmd(sock, "AUTH LOGIN")
    recv(sock)
    send_cmd(sock, base64.b64encode(SENDER_EMAIL.encode()).decode())
    recv(sock)
    send_cmd(sock, base64.b64encode(APP_PASSWORD.encode()).decode())
    recv(sock)

    # Buoc 5: MAIL FROM - khai bao nguoi gui (SMTP envelope, khac voi header From)
    send_cmd(sock, f"MAIL FROM:<{SENDER_EMAIL}>")
    recv(sock)

    # Buoc 6: RCPT TO - khai bao nguoi nhan
    send_cmd(sock, f"RCPT TO:<{RECEIVER_EMAIL}>")
    recv(sock)

    # Buoc 7: DATA - bat dau gui noi dung email, ket thuc bang dong chi co dau "."
    send_cmd(sock, "DATA")
    recv(sock)

    email_content = (
        f"Subject: Demo SMTP - Raw Socket\r\n"
        f"From: {SENDER_EMAIL}\r\n"
        f"To: {RECEIVER_EMAIL}\r\n"
        f"\r\n"
        f"Day la noi dung email gui bang socket tho, tu go tung lenh SMTP.\r\n"
        f".\r\n"  # dong "." bao hieu ket thuc noi dung DATA
    )
    sock.sendall(email_content.encode())
    recv(sock)

    # Buoc 8: QUIT - dong phien lam viec
    send_cmd(sock, "QUIT")
    recv(sock)
    sock.close()
    print("Da gui email thanh cong bang socket tho (khong dung smtplib).")


if __name__ == "__main__":
    raw_smtp_send()
