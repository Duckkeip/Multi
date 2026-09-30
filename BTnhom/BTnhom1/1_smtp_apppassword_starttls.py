"""
Gui email co ban qua SMTP Gmail, dung App Password + STARTTLS (port 587).

"""

import smtplib
from email.mime.text import MIMEText

SMTP_SERVER = "smtp.gmail.com"
SMTP_PORT = 587  # STARTTLS - ma hoa duoc bat sau khi ket noi

SENDER_EMAIL = "doggygerman@gmail.com"
APP_PASSWORD = "cxkz xobv qnst fyxh"  # App Password, khong phai mat khau that
RECEIVER_EMAIL = "2331540141@vaa.edu.vn"


def send_simple_email():
    msg = MIMEText("Day la noi dung email demo gui qua SMTP + App Password.")
    msg["Subject"] = "Demo SMTP - App Password (STARTTLS 587)"
    msg["From"] = SENDER_EMAIL
    msg["To"] = RECEIVER_EMAIL

    # smtplib.SMTP dung port 587, ket noi plain truoc, sau do nang cap len TLS
    with smtplib.SMTP(SMTP_SERVER, SMTP_PORT) as server:
        server.ehlo()
        server.starttls()  # nang cap ket noi len ma hoa TLS
        server.ehlo()
        server.login(SENDER_EMAIL, APP_PASSWORD)
        server.sendmail(SENDER_EMAIL, [RECEIVER_EMAIL], msg.as_string())
        print("Da gui email thanh cong qua port 587 (STARTTLS).")


if __name__ == "__main__":
    send_simple_email()
