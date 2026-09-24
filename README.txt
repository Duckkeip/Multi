lỗi the remote certificate is 
invalid because of errors in 
the certificate chain : UntrustedRoot


Cài đặt Certificate vào Trusted Root CA của Windows (Khuyên dùng khi Dev)Nếu không muốn sửa code bỏ qua kiểm tra an toàn, 
bạn có thể thêm file certificate của Server vào máy client:   
-Nhấn Win + R, gõ certmgr.msc và nhấn Enter.
-Tìm đến thư mục Trusted Root Certification Authorities > Certificates.
-Nhấp chuột phải > All Tasks > Import...Chọn file .crt / .cer / .pfx của Server và tiến hành cài đặt vào store Trusted Root Certification Authorities.