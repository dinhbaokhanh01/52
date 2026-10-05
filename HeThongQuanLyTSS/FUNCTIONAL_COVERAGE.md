# Đối chiếu Product Backlog

Nguồn: He_thong_quan_ly_Thuc_tap_sinh_TTCS_T926_K10S6_Da_Phan_Cong.xlsx, sheet Product Backlog. Dòng có màu/không STT là Epic; dòng có STT là user story. Bảng dưới mô tả chức năng đã hiện thực, không khẳng định đã kiểm thử mọi tình huống nghiệm thu.

| Epic | STT | Phạm vi trong bản bổ sung |
|---|---|---|
| Quản lý hồ sơ thực tập sinh | 1–3 | Giữ màn hình HR thêm/sửa/tìm/lọc hồ sơ; bổ sung bảo vệ đăng nhập. |
| Quản lý hồ sơ thực tập sinh | 4–5 | Giữ tải CV/đơn, xem/duyệt tài liệu; kiểm tra quyền sở hữu khi tải. |
| Tiếp nhận và xét duyệt | 6 | Đăng ký, xác thực email, đăng nhập và nộp hồ sơ. |
| Tiếp nhận và xét duyệt | 7–8 | Duyệt/từ chối có lý do, thông báo ứng dụng và email hàng đợi. |
| Tiếp nhận và xét duyệt | 9–10 | Giữ tải hợp đồng, xác nhận và lịch sử. |
| Quản lý chương trình thực tập | 11 | Chương trình/phòng ban/chỉ tiêu. |
| Quản lý chương trình thực tập | 12 | Phân công mentor, kiểm tra sức chứa và phòng ban. |
| Quản lý chương trình thực tập | 13–14 | Ngày bắt đầu/kết thúc; thực tập sinh xem chương trình, phân công và lịch làm việc của mình. |
| Quản lý công việc & đánh giá | 15–16 | Mentor giao việc, thực tập sinh cập nhật tiến độ 0–100%. |
| Quản lý công việc & đánh giá | 17–18 | Báo cáo tuần, tệp đính kèm, kiểm tra tuần trùng, mentor phản hồi. |
| Quản lý công việc & đánh giá | 19–20 | Điểm kỹ năng/thái độ và nhận xét; bảng đánh giá tổng hợp, Excel và in PDF. |
| Quản lý chấm công & thời gian | 21–22 | Vào/ra theo ngày; tổng hợp lượt chấm công, giờ làm và đơn nghỉ duyệt. |
| Quản lý chấm công & thời gian | 23–24 | Ca làm theo từng người, kiểm tra giao nhau; đơn nghỉ và quyết định HR. Chưa có thao tác tạo ca hàng loạt cho cả nhóm. |
| Quản lý hỗ trợ & quyền lợi | 25–26 | Phụ cấp theo kỳ, đánh dấu thanh toán, lịch sử; thực tập sinh xem dữ liệu của mình. Không kết nối ngân hàng. |
| Quản lý hỗ trợ & quyền lợi | 27–28 | Yêu cầu hỗ trợ kèm tệp; HR duyệt/từ chối/đóng và phản hồi. |
| Quản lý mentor & phòng ban | 29–30 | Hồ sơ mentor liên kết tài khoản/phòng ban, phân công thực tập sinh. |
| Quản lý mentor & phòng ban | 31 | Số người được phân công và sức chứa từng mentor. |
| Báo cáo & thống kê | 32–33 | Số lượng trường/ngành, tỷ lệ hoàn thành theo trạng thái hồ sơ. API hỗ trợ lọc chương trình; giao diện lọc trường/ngành. |
| Báo cáo & thống kê | 34 | Excel hồ sơ/đánh giá; lưu PDF qua hộp thoại in của trình duyệt, không xuất PDF nền ở server. |
| Tích hợp & thông báo | 35–36 | Lịch họp xếp email, thông báo ứng dụng và đánh dấu đã đọc. Chưa có push notification khi đóng ứng dụng. |
| Tích hợp & thông báo | 37 | API nhận/upsert dữ liệu HRM theo mã nhân sự, màn hình nhập JSON. Cần adapter nhà cung cấp để đồng bộ tự động thực tế. |
| Tích hợp & thông báo | 38 | API sự kiện chấm công QR/thẻ chống trùng eventId. Cần thiết bị/adapter thực tế; không quét camera trong web. |
| Quản trị hệ thống | 39 | Admin tạo HR/Mentor/Admin; hồ sơ thực tập sinh tạo qua trang HR, Admin đặt mật khẩu/kích hoạt/khóa; thực tập sinh cũng tự đăng ký. |
| Quản trị hệ thống | 40 | Vai trò và phạm vi hồ sơ; danh sách quyền ghi workflow tùy tài khoản. API hồ sơ cũ dùng quyền HR/Admin, chưa có ma trận quyền từng trường. |
| Quản trị hệ thống | 41 | Sao lưu ứng dụng mã hóa định kỳ khi server chạy, tạo/tải thủ công, phục hồi vào database mới. |
| Quản trị hệ thống | 42 | Nhật ký thao tác API, người thực hiện, tài nguyên, mã kết quả; màn hình Admin. |

## Lưu ý khi nghiệm thu

- Thử với ít nhất một tài khoản mỗi vai trò và dữ liệu theo chương trình của bạn.
- SMTP mặc định chỉ tạo file .eml. Cấu hình SMTP thật mới gửi đến hộp thư.
- Story 37–38 mới có đầu nhận tích hợp, chưa thể nghiệm thu kết nối thực tế khi chưa biết hệ thống/thiết bị đích.
- Sao lưu không chạy khi ứng dụng tắt; muốn hoạt động liên tục cần triển khai thành dịch vụ và giám sát.
- Đọc README về cài đặt, khóa sao lưu và giới hạn bảo mật trước triển khai.
