# Xây dựng website đặt phòng khách sạn

## Đề tài: Xây dựng website đặt phòng khách sạn (ASP.NET)

Đồ án chuyên đề ASP.NET xây dựng website đặt phòng khách sạn với cổng khách hàng đặt phòng online và cổng nhân viên/quản trị, được phát triển bằng **C# / ASP.NET Core MVC** kết nối **Microsoft SQL Server** qua **Entity Framework Core**.

---

# Thông tin đồ án

| Thông tin                | Chi tiết                                                                   |
| ------------------------ | -------------------------------------------------------------------------- |
| **Trường**               | Đại học Trà Vinh – Trường Kỹ thuật và Công nghệ – Khoa Công nghệ Thông tin |
| **Lớp**                  | DK24TT8016                                                                 |
| **Giảng viên hướng dẫn** | TS. Đoàn Phước Miên                                                        |
| **Năm học**              | 2026                                                                       |

### Nhóm sinh viên thực hiện

| STT | Họ và tên        | MSSV      |
| --: | ---------------- | --------- |
|   1 | Lê Thị Ái Lanh   | 170124184 |
|   2 | Nguyễn Văn Thủy  | 170124292 |

---

# 1. Giới thiệu

Website đặt phòng khách sạn cho phép khách hàng tìm phòng trống, đặt phòng online; đồng thời cung cấp cổng nhân viên/quản trị giúp quản lý toàn bộ hoạt động của khách sạn (phòng, nhân viên, hóa đơn lưu trú, báo cáo doanh thu, nhật ký hoạt động).

### Công nghệ sử dụng

* **Ngôn ngữ lập trình:** C#
* **Nền tảng:** ASP.NET Core MVC
* **Cơ sở dữ liệu:** Microsoft SQL Server
* **Truy xuất dữ liệu:** Entity Framework Core (`Microsoft.EntityFrameworkCore.SqlServer`)
* **Giao diện:** HTML5, CSS3, JavaScript + Bootstrap + jQuery
* **Xác thực:** Session cookie
* **Phân quyền:** 3 vai trò nhân viên (Admin / Lễ tân / Tạp vụ) + cổng khách hàng riêng
* **Quy trình phát triển:** Waterfall
* **Công cụ phát triển:** Visual Studio, SQL Server Management Studio (SSMS), Git

---

# 2. Chức năng chính

## Cổng Khách hàng (Web Portal)

* **Trang chủ & Giới thiệu:** Xem thông tin phòng, bảng giá dịch vụ và thông tin liên hệ.
* **Tài khoản khách hàng:** Đăng ký, đăng nhập, cập nhật hồ sơ, đổi mật khẩu.
* **Tìm kiếm & Đặt phòng Online:** Tìm phòng trống theo khoảng ngày nhận/trả và số lượng khách; gửi yêu cầu đặt phòng, chờ nhân viên xác nhận.
* **Quản lý đơn cá nhân:** Tra cứu lịch sử đặt phòng, chi tiết đơn, hủy đơn hợp lệ (trước thời điểm nhận phòng ít nhất 12 giờ).

## Cổng Nhân viên & Quản trị (Admin Portal)

* **Admin:**
  * Dashboard theo thời gian thực (số phòng trống, đang ở, đang dọn, bảo trì, doanh thu hôm nay).
  * Quản lý danh mục phòng, loại phòng và gallery ảnh phòng.
  * Quản lý tài khoản nhân viên và phân quyền (Admin, Lễ tân, Tạp vụ).
  * Báo cáo doanh thu theo tháng và thống kê tần suất thuê phòng.
  * Xem nhật ký hoạt động (Audit Logs) của toàn bộ nhân viên.
* **Lễ tân (LeTan):**
  * Quản lý thông tin khách hàng và lịch sử lưu trú.
  * Quản lý đơn đặt phòng online: xác nhận đơn, hủy đơn, check-in từ đơn online sang hóa đơn lưu trú.
  * Lập phiếu nhận phòng (Check-in), gọi thêm dịch vụ, thanh toán trả phòng (Check-out).
* **Tạp vụ (TapVu):**
  * Theo dõi danh sách phòng cần dọn dẹp hoặc đang bảo trì theo tầng.
  * Cập nhật trạng thái hoàn tất dọn phòng (chuyển về Trống) hoặc báo bảo trì phòng.

---

# 3. Kiến trúc

Connection string duy nhất đọc từ `appsettings.json`.

### Stack

| Thành phần        | Công nghệ                                  |
| ----------------- | ------------------------------------------ |
| Framework         | ASP.NET Core MVC                           |
| Data access       | Entity Framework Core                      |
| DB                | SQL Server                                 |
| Auth              | Session cookie                             |
| Views             | Razor + Tag Helpers                        |
| Phân quyền        | 3 vai trò nhân viên + cổng khách hàng riêng |

---

# 4. Cấu trúc thư mục

```text
qlks/
├── progress-report/      # Báo cáo tiến độ
│   ├── Tuan1.md
│   ├── Tuan2.md
│   ├── Tuan3.md
│   └── Tuan4.md
│
├── setup/
│   └── setup.sql         # Script tạo cơ sở dữ liệu
│
├── src/                  # Mã nguồn ASP.NET Core MVC
│   ├── Controllers/      # 13 controller (khách hàng + nhân viên/quản trị)
│   ├── Data/             # AppDbContext + Entities (14 bảng)
│   ├── Infrastructure/   # Cấu hình, phân quyền, hằng số, MVC compatibility
│   ├── Models/           # ViewModels
│   ├── Services/         # Business services (đặt phòng, mật khẩu, ảnh, audit...)
│   ├── Views/            # Razor views (khách hàng + quản trị) + Layout
│   ├── Content/          # CSS & hình ảnh
│   ├── Scripts/          # JavaScript
│   ├── GlobalUsings.cs
│   ├── Program.cs
│   ├── appsettings.json
│   └── QLKS.csproj / QLKS.sln
│
└── thesis/
    └── ASPNET-DK24TT8016-QLKS-HeThongQuanLyKhachSan.pdf
```

### Báo cáo đồ án (thư mục `thesis/`)

Thư mục này chứa **báo cáo đồ án** (file Word .docx hoặc PDF).

Cách đặt tên file (theo mẫu nộp bài):

```text
ASPNET-<Lop>-<TenNhom>-<TenDeTai>.docx
ASPNET-<Lop>-<TenNhom>-<TenDeTai>.pdf
```

Ví dụ: `ASPNET-DK24TT8016-QLKS-HeThongQuanLyKhachSan.docx`

Lưu ý: Đưa file báo cáo (.docx/.pdf) vào thư mục này trước khi nộp.

---

# 5. Hướng dẫn cài đặt

## Yêu cầu

* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* Visual Studio 2022 hoặc VS Code
* Microsoft SQL Server (LocalDB hoặc instance bất kỳ)
* SQL Server Management Studio (SSMS)

## Các bước cài đặt

### Bước 1. Tải mã nguồn

```bash
git clone <repository-url>
cd qlks
```

### Bước 2. Tạo cơ sở dữ liệu

Chạy script tạo database + seed dữ liệu mẫu:

```bash
sqlcmd -S . -i setup/setup.sql
```

Script `setup.sql` tự động:

* Drop + tạo lại DB `QLKS`.
* Tạo 14 bảng, khóa ngoại, CHECK constraints (ngày trả > ngày nhận, trạng thái đơn hợp lệ), index, 4 stored procedures, 2 triggers.
* Seed dữ liệu mẫu: 3 loại phòng, 4 phòng, dịch vụ, 3 tài khoản nhân viên theo vai trò (Admin/LeTan/TapVu, mật khẩu 123), 1 khách hàng mẫu (mật khẩu 123456), 1 hóa đơn mẫu.

### Bước 3. Cấu hình chuỗi kết nối

Sửa `src/appsettings.json` cho đúng SQL Server của bạn:

```json
"ConnectionStrings": {
  "QLKS": "Server=.;Database=QLKS;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

### Bước 4. Chạy chương trình

```bash
cd src
dotnet restore
dotnet build
dotnet run
```

Mở trình duyệt theo URL hiển thị trên terminal (mặc định `http://localhost:5000` hoặc cổng được cấp phát).

---

# 6. Tài khoản dùng thử

| Vai trò       | Tên đăng nhập          | Mật khẩu mặc định        | Cổng truy cập     |
| ------------- | ---------------------- | ------------------------ | ----------------- |
| **Admin**     | `admin`                | `123`    | `/Account/Login`  |
| **Lễ tân**    | `letan01`              | `123`| `/Account/Login`  |
| **Tạp vụ**    | `tapvu01`              | `123` | `/Account/Login` |
| **Khách hàng** | `khachhang@example.com` | `123456`    | `/CustomerAccount/Login` |

Tài khoản trên đã được tạo sẵn trong `setup/setup.sql`. Có thể tạo thêm thông qua chức năng **Đăng ký**.

---

# 7. Các đường dẫn chính

- **Trang chủ khách hàng:** `/`
- **Tìm & Đặt phòng:** `/OnlineBooking/Search`
- **Đơn đặt của tôi:** `/OnlineBooking/MyBookings`
- **Đăng nhập khách hàng:** `/CustomerAccount/Login`
- **Đăng ký khách hàng:** `/CustomerAccount/Register`
- **Đăng nhập nhân viên:** `/Account/Login`
- **Dashboard Quản trị:** `/Home/Index`
- **Quản lý đơn Online (Admin/Lễ tân):** `/OnlineBookingAdmin/Index`
- **Quản lý phòng:** `/Phong/Index`
- **Quản lý hóa đơn:** `/HoaDon/Index`
- **Quản lý khách hàng:** `/KhachHang/Index`
- **Quản lý nhân viên (Admin):** `/NhanVien/Index`
- **Báo cáo doanh thu (Admin):** `/Admin/BaoCao`
- **Nhật ký hoạt động (Admin):** `/Admin/Log`
- **Nghiệp vụ tạp vụ:** `/TapVu/Index`

---

# 8. Báo cáo tiến độ

Các báo cáo tiến độ được lưu trong thư mục:

```text
progress-report/
```

| Tuần    | Thời gian               | Nội dung                                                |
| ------- | ----------------------- | ------------------------------------------------------- |
| Tuần 1  | 01/08/2026 – 07/08/2026 | Khảo sát và phân tích yêu cầu                           |
| Tuần 2  | 08/08/2026 – 14/08/2026 | Thiết kế cơ sở dữ liệu và ERD                           |
| Tuần 3  | 15/08/2026 – 21/08/2026 | Dựng khung dự án, Models/Entities & Controllers         |
| Tuần 4  | 19/08/2026 – 25/08/2026 | Hoàn thiện giao diện, kiểm thử và bàn giao              |

---

# 9. Kết quả đạt được

Sau quá trình thực hiện, nhóm đã hoàn thành các nội dung sau:

* Thiết kế và xây dựng cơ sở dữ liệu trên Microsoft SQL Server (14 bảng, stored procedures, triggers, index).
* Xây dựng đầy đủ chức năng quản trị (CRUD) cho phòng, loại phòng, nhân viên, khách hàng, dịch vụ, hóa đơn.
* Hoàn thiện cổng khách hàng: tìm phòng, đặt phòng online, tra cứu/hủy đơn.
* Hoàn thiện luồng check-in từ đơn online sang hóa đơn lưu trú.
* Phân quyền 3 vai trò nhân viên + cổng khách hàng riêng.
* Xác thực Session + phân quyền 3 vai trò nhân viên.
* Báo cáo doanh thu theo tháng, thống kê tần suất phòng, nhật ký hoạt động.
* Hoàn thiện báo cáo đồ án và tài liệu hướng dẫn cài đặt.

---

# 10. Hướng phát triển

Trong tương lai, hệ thống có thể được mở rộng với các chức năng:

* Đăng nhập bằng Google hoặc Facebook.
* Giữ chỗ tự động: tạm giữ phòng một khoảng thời gian khi khách đặt, tự hủy nếu không xác nhận.
* Thanh toán cọc trực tuyến: tích hợp cổng thanh toán (VNPay, Momo) để thu cọc trước khi xác nhận đơn.
* Thanh toán cổng thực tế (VNPay, Momo, Stripe).
* Gửi email nhắc check-in/check-out tự động.
* Biểu đồ doanh thu nâng cao và dashboard phân tích.
* Đánh giá/phản hồi của khách hàng sau lưu trú.
* Quản lý khuyến mãi và chương trình khách hàng thân thiết.

---

**Đồ án Chuyên đề ASP.NET – Xây dựng website đặt phòng khách sạn**
**Lớp:** DK24TT8016
**Giảng viên hướng dẫn:** TS. Đoàn Phước Miền
**Năm học:** 2026