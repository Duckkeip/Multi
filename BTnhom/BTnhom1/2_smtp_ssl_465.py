"""
Gui email qua SMTP_SSL, port 465.
Khac voi port 587 (STARTTLS - nang cap ma hoa sau khi ket noi),
port 465 ma hoa SSL/TLS ngay tu luc bat dau ket noi (implicit TLS).

"""

import smtplib
from email.mime.text import MIMEText

SMTP_SERVER = "smtp.gmail.com"
SMTP_PORT = 465  # SSL/TLS ngay tu dau, khong can goi starttls()

SENDER_EMAIL = "doggygerman@gmail.com"
APP_PASSWORD = "cxkz xobv qnst fyxh"
RECEIVER_EMAIL = "2331540141@vaa.edu.vn"


def send_email_ssl():
    msg = MIMEText("Noi dung email demo gui qua port 465 (SSL truc tiep).")
    msg["Subject"] = "Demo SMTP - SSL truc tiep (Port 465)"
    msg["From"] = SENDER_EMAIL
    msg["To"] = RECEIVER_EMAIL

    # SMTP_SSL: ma hoa ngay khi ket noi, khong can starttls()
    with smtplib.SMTP_SSL(SMTP_SERVER, SMTP_PORT) as server:
        server.login(SENDER_EMAIL, APP_PASSWORD)
        server.sendmail(SENDER_EMAIL, [RECEIVER_EMAIL], msg.as_string())
        print("Da gui email thanh cong qua port 465 (SSL).")


if __name__ == "__main__":
    send_email_ssl()
