"""
Gui email dang MIME: co noi dung HTML + file dinh kem.
Minh hoa chuan MIME (Multipurpose Internet Mail Extensions) - phan quan trong
cua giao thuc email de gui duoc noi dung phong phu, khong chi text thuan.
"""

import smtplib
from email.mime.multipart import MIMEMultipart
from email.mime.text import MIMEText
from email.mime.application import MIMEApplication

SMTP_SERVER = "smtp.gmail.com"
SMTP_PORT = 587

SENDER_EMAIL = "doggygerman@gmail.com"
APP_PASSWORD = "cxkz xobv qnst fyxh"
RECEIVER_EMAIL = "receiver@example.com"

ATTACHMENT_PATH = "bao_cao.pdf"  # doi thanh duong dan file that khi demo


def send_mime_email():
    # "mixed": cho phep vua co phan noi dung (alternative) vua co file dinh kem
    msg = MIMEMultipart("mixed")
    msg["Subject"] = "Demo SMTP - MIME (HTML + Dinh kem)"
    msg["From"] = SENDER_EMAIL
    msg["To"] = RECEIVER_EMAIL

    # Phan noi dung: co ca ban text thuong va ban HTML
    body = MIMEMultipart("alternative")
    plain_text = "Day la phien ban text thuong, hien thi khi mail client khong doc duoc HTML."
    html_text = """
    <html>
      <body>
        <h2 style="color:#306998;">Demo email HTML qua SMTP</h2>
        <p>Day la noi dung <b>HTML</b> minh hoa cho de tai SMTP.</p>
      </body>
    </html>
    """
    body.attach(MIMEText(plain_text, "plain"))
    body.attach(MIMEText(html_text, "html"))
    msg.attach(body)

    # Phan dinh kem file (neu co)
    try:
        with open(ATTACHMENT_PATH, "rb") as f:
            part = MIMEApplication(f.read(), Name=ATTACHMENT_PATH)
        part["Content-Disposition"] = f'attachment; filename="{ATTACHMENT_PATH}"'
        msg.attach(part)
    except FileNotFoundError:
        print(f"Khong tim thay file '{ATTACHMENT_PATH}', bo qua phan dinh kem.")

    with smtplib.SMTP(SMTP_SERVER, SMTP_PORT) as server:
        server.starttls()
        server.login(SENDER_EMAIL, APP_PASSWORD)
        server.sendmail(SENDER_EMAIL, [RECEIVER_EMAIL], msg.as_string())
        print("Da gui email MIME (HTML + dinh kem) thanh cong.")


if __name__ == "__main__":
    send_mime_email()
