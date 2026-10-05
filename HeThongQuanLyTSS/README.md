# Hệ thống quản lý thực tập sinh — bản bổ sung

Bổ sung trên demo được cung cấp, đối chiếu 42 user story của Product Backlog theo 10 Epic. Các chức năng mới tích hợp ngay trong menu và bố cục của demo cũ, không dùng cổng nghiệp vụ tách biệt hay iframe. Giữ màn hình đăng nhập, hồ sơ, tài liệu, xét duyệt và hợp đồng cũ. Không lấy tiêu đề Epic làm user story và không lấy các story chỉ xuất hiện ở Sprint.

## Bản tích hợp giao diện demo

HR/Admin dùng hr.html; thực tập sinh dùng thuc-tap-sinh.html; Mentor dùng mentor.html theo cùng mẫu giao diện. Sau đăng nhập hệ thống tự chuyển đúng vai trò. Menu bổ sung gồm chương trình, phân công, công việc, báo cáo, lịch, chấm công, nghỉ phép, phụ cấp, hỗ trợ và các mục quản trị theo quyền. Giao diện dùng lại bảng màu xanh, font, sidebar và topbar của demo. index.html là điểm vào/đăng ký/khởi tạo, không còn là màn hình nghiệp vụ riêng.

Để cập nhật: dừng bản đang chạy bằng Ctrl+C, giải nén ZIP mới vào một thư mục mới, mở trong VS Code và chạy start.ps1. Script mặc định dùng cùng database CareerPortalExtended nên dữ liệu đã nhập ở database này được giữ. Nếu bản cũ dùng tên khác, truyền đúng tên qua -Database. Không xóa thư mục App_Data/keys của bản cũ: cần giữ khóa để đọc bản sao lưu mã hóa cũ; khi chuyển thư mục chạy hãy sao chép App_Data của bản cũ sang vị trí tương ứng trước khi chạy. Không cần xóa hoặc tạo lại database.

Đã kiểm tra giao diện bằng tài khoản thử Admin, Mentor và thực tập sinh: đăng nhập, menu theo vai trò, mở công việc/báo cáo, biểu mẫu và quay về hồ sơ gốc. Launcher kèm theo đã sửa mã hóa PowerShell 5.1 và thêm dotnet restore trước migration.

## Chạy trên Windows

Yêu cầu: .NET SDK 10, SQL Server LocalDB (MSSQLLocalDB), dotnet-ef 10.0.12. Không cần Node/Python để chạy ứng dụng.

1. Giải nén vào thư mục có quyền ghi.
2. Nếu chưa có EF CLI: `dotnet tool install --global dotnet-ef --version 10.0.12`.
3. Mở PowerShell tại thư mục chứa file này, chạy `./start.ps1`.
4. Mở http://localhost:5134/index.html. Mở mục tạo Admin lần đầu, nhập mã in trong terminal và thông tin tài khoản của bạn. Mật khẩu tối thiểu 12 ký tự.
5. Admin tạo tài khoản HR/Mentor; thực tập sinh dùng mục đăng ký. Tạo tài khoản Mentor trước rồi liên kết khi tạo hồ sơ mentor. Tạo chương trình, mentor và phân công trước khi giao việc.

Database mặc định **CareerPortalExtended**, không ghi đè database CareerPortal cũ. Đổi tên bằng `./start.ps1 -Database TenDatabase -Port 5134`. Chỉ chọn database cũ khi đã sao lưu và muốn áp dụng migration lên đó. Dữ liệu cũ được giữ qua migration.

Không có tài khoản/mật khẩu mặc định trong bản bàn giao. Không mở HTML bằng file://. Backend phục vụ cả giao diện/API trên cùng cổng. Dừng bằng Ctrl+C.

## Email demo và email thật

Mặc định script lưu thư vào `backend/Mã nguồn/CareerPortal.Api/App_Data/mail/*.eml`, không gửi ra Internet. Đợi tối đa khoảng 30 giây rồi mở file .eml để xem thư/link xác thực. Admin có màn hình hàng đợi thư; SavedToPickup nghĩa là chỉ lưu demo, Sent là đã chuyển cho SMTP. Tài khoản đăng ký mới phải xác thực email trước khi nộp hồ sơ.

Để gửi thật, đặt biến môi trường trước khi chạy; không ghi mật khẩu vào mã nguồn:

```powershell
$env:Smtp__Host = 'smtp.your-organization.example'
$env:Smtp__Port = '587'
$env:Smtp__EnableSsl = 'true'
$env:Smtp__From = 'portal@your-organization.example'
$env:Smtp__Username = 'your-smtp-account'
$env:Smtp__Password = 'your-smtp-password'
./start.ps1
```

Thư lỗi thử lại tối đa 5 lần. Email xét duyệt thuộc story 8/Epic Tiếp nhận và xét duyệt; email lịch họp thuộc story 35/Epic Tích hợp & thông báo. Triển khai ngoài localhost cần cấu hình PublicUrl/HTTPS thay vì dùng nguyên script demo.

## Các nhóm chức năng

- Hồ sơ, đăng ký/xác thực, CV/đơn, xét duyệt, hợp đồng và xác nhận.
- Chương trình, số lượng tiếp nhận, mentor/phòng ban, phân công và kiểm tra sức chứa.
- Công việc/tiến độ, báo cáo tuần kèm tệp, phản hồi và đánh giá.
- Ca làm, lịch cá nhân, vào/ra, đơn nghỉ, tổng hợp chấm công.
- Trợ cấp/thanh toán/lịch sử; yêu cầu hỗ trợ và phản hồi.
- Thống kê trường/ngành/chương trình, tải Excel, in/lưu PDF từ trình duyệt.
- Lịch họp, thông báo, email, API nhập HRM/chấm công thiết bị.
- Tài khoản, vai trò/phạm vi dữ liệu, sao lưu/phục hồi và nhật ký.

Xem FUNCTIONAL_COVERAGE.md về phạm vi từng story và giới hạn.

## Sao lưu và phục hồi

Sao lưu ứng dụng mã hóa tại App_Data/backups, tự động 24 giờ một lần khi ứng dụng đang chạy; Admin cũng có thể tạo/tải thủ công. Chứa dữ liệu, tài khoản, lịch sử và tệp đính kèm. Đây là sao lưu ứng dụng, không phải SQL Server .bak.

Phục hồi tạo database mới CareerPortalRestore_..., không thay database đang dùng; thư chờ được tạm dừng để tránh gửi lại. Kiểm tra dữ liệu rồi chạy script với tên database mới nếu muốn sử dụng. Phải giữ an toàn cả App_Data/keys cùng bản sao lưu; mất khóa sẽ không giải mã được. Phục hồi sang máy khác cần chuyển khóa và cấu hình Data Protection phù hợp. ZIP không kèm khóa, thư hay dữ liệu kiểm thử.

## Tích hợp và giới hạn

Màn hình Tích hợp cho Admin nhập JSON theo ví dụ. API nhận dữ liệu HRM/sự kiện chấm công có kiểm tra và chống trùng. Chưa có hãng HRM, giao thức máy đọc thẻ hoặc thông tin truy cập nên chưa có kết nối tự động với dịch vụ/thiết bị cụ thể; cần adapter gọi các API này. Không phải chức năng quét QR bằng camera.

Đây là demo mở rộng, chưa chứng nhận sẵn sàng production. Trước vận hành thật cần rà soát bảo mật, HTTPS, giới hạn đăng nhập, secret/khóa, giám sát, lưu giữ dữ liệu và kiểm thử theo chính sách tổ chức. Quyền chi tiết trong màn hình tài khoản áp dụng cho thao tác workflow; API hồ sơ cũ áp dụng vai trò HR/Admin và quyền sở hữu, chưa phân quyền từng trường dữ liệu.

## Kiểm tra đã thực hiện

Build .NET; kiểm tra cú pháp JavaScript; 70 kiểm tra HTTP và 4 thư qua SMTP thử nghiệm; 9 kiểm tra bổ sung về xung đột cập nhật, ràng buộc xóa, lưu bền và phục hồi; đối chiếu SQL phục hồi gồm hồ sơ, tài khoản, tiến độ, tệp và lịch sử. Mã kiểm tra trong tests/.

Test chỉ dùng database/tài khoản giả. smoke.py cần API localhost:5148 với database mới, BootstrapToken thử trong mã và SMTP localhost:2526. final_checks.py chạy sau smoke; project VerifyRestore kiểm tra database do final_checks tạo. Kết quả thực thi sinh tại máy chạy, không kèm dữ liệu thử trong ZIP.
