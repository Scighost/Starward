[English](./Configuration.md) | [简体中文](./Configuration.zh-CN.md) | Tiếng Việt | [日本語](./Configuration.ja-JP.md)

# Cấu hình ứng dụng

Kể từ phiên bản 0.8.0, Starward sẽ không còn sử dụng registry để lưu trữ cấu hình, mà thay vào đó sử dụng tập tin và cơ sở dữ liệu, giúp việc di chuyển tổng thể ứng dụng giữa các thiết bị trở nên dễ dàng hơn. Tuy nhiên, registry sẽ vẫn được sử dụng khi cấu trúc tập tin không đáp ứng các điều kiện sau:

```
│ config.ini
│ starward.exe
│ version.ini
│
└─ app-0.8.0
   │ Starward.exe
   ...
```

Đừng lo lắng, các tập tin tải về từ Github Release sẽ thoả mãn điều kiện này, chỉ khi bạn pull code và debug cục bộ thì mới sử dụng registry để lưu trữ cấu hình.

## config.ini

Tập tin `config.ini` chỉ chứa hai mục cài đặt:

```ini
# Có bật ghi nhật ký đầu ra của console hay không, True/False
EnableConsole=False
# Vị trí thư mục người dùng
UserDataFolder=.
```

`UserDataFolder` là thư mục chứa dữ liệu người dùng. Nếu giá trị này không tồn tại hoặc thư mục đã đặt không tồn tại, ứng dụng sẽ hiển thị trang chào mừng khi khởi động. Nếu `UserDataFolder` được đặt thành chính thư mục hoặc thư mục con chứa tập tin `config.ini`, bạn có thể sử dụng **đường dẫn tương đối**, ví dụ: dấu chấm `.` đại diện cho thư mục hiện tại. Trong các trường hợp khác, bạn **phải** sử dụng một đường dẫn tuyệt đối. Ngoài ra, cả dấu gạch chéo `/` và dấu gạch chéo ngược `\` đều có thể được sử dụng.

Lưu ý: Tập tin `config.ini` phải nằm trong thư mục gốc của ứng dụng.

## Database

Tất cả các mục cài đặt ngoại trừ hai mục trên đều được lưu trữ trong cơ sở dữ liệu `StarwardDatabase.db` ở thư mục người dùng. Tập tin này là tập tin cơ sở dữ liệu SQLite, mà bạn có thể chỉnh sửa bằng [DB Browser for SQLite](https://sqlitebrowser.org/) hoặc các phần mềm khác.

Sẽ có một bảng tên là `Setting` ở database chứa những mục cài đặt ứng dụng, và nó có cấu trúc như sau, với các key và value được biểu thị dưới dạng văn bản.

```sql
CREATE TABLE Setting
(
    Key TEXT NOT NULL PRIMARY KEY.
    Value TEXT
).
```

Có hai loại mục cài đặt trong ứng dụng, mục cài đặt tĩnh sử dụng danh pháp Pascal `ASettingKey`, và mục cài đặt động sử dụng danh pháp Pascal `a_setting_key`, biểu thị sự tồn tại của một giá trị tương ứng cho từng khu vực trò chơi.

## Khu vực trò chơi

Starward sử dụng `enum GameBiz` để xác định các khu vực trò chơi khác nhau, trong đó có tên đầy đủ của trò chơi như `StarRail` sẽ được chỉ định khi được sử dụng.

| Key               | Value | Chú thích                                   |
| ----------------- | ----- | ------------------------------------------- |
| None              | 0     | Giá trị mặc định                            |
| All               | 1     | Tất cả                                      |
| **GenshinImpact** | 10    | Genshin Impact                              |
| hk4e_cn           | 11    | Genshin Impact (Trung Quốc Đại Lục)         |
| hk4e_global       | 12    | Genshin Impact (Toàn Cầu)                   |
| hk4e_cloud        | 13    | Genshin Impact · Cloud (Trung Quốc Đại Lục) |
| **StarRail**      | 20    | Honkai: Star Rail                           |
| hkrpg_cn          | 21    | Star Rail (Trung Quốc Đại Lục)              |
| hkrpg_global      | 22    | Star Rail (Toàn Cầu)                        |
| **Honkai3rd**     | 30    | Honkai 3rd                                  |
| bh3_cn            | 31    | Honkai 3rd (Trung Quốc Đại Lục)             |
| bh3_global        | 32    | Honkai 3rd (Toàn Cầu)                       |
| bh3_jp            | 33    | Honkai 3rd (Nhật Bản)                       |
| bh3_kr            | 34    | Honkai 3rd (Hàn Quốc)                       |
| bh3_overseas      | 35    | Honkai 3rd (Đông Nam Á)                     |
| bh3_tw            | 36    | Honkai 3rd (TW/HK/MO)                       |

## Cài đặt tĩnh (Static Settings)

Kiểu dữ liệu `Type` trong bảng sau sử dụng biểu thức trong C#, và `-` biểu thị giá trị mặc định của loại này.

| Key                             | Kiểu    | Giá trị mặc định | Chú thích                                                                                                                                                         |
| ------------------------------- | ------- | ---------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Language                        | string? | -                | Ngôn ngữ giao diện ứng dụng, như `zh-CN` `en-US`, sẽ theo cài đặt hệ thống nếu bỏ trống.                                                                          |
| WindowSizeMode                  | int     | -                | Kích cỡ ứng dụng, 0 - Tiêu chuẩn, 1 - Nhỏ                                                                                                                         |
| ApiCDNIndex                     | int     | -                | Tuỳ chọn API CDN, 0 - CloudFlare, 1 - GitHub, 2 - jsDelivr                                                                                                        |
| EnablePreviewRelease            | bool    | -                | Có tham gia kênh phát hành xem trước hay không.                                                                                                                   |
| IgnoreVersion                   | string? | -                | Bỏ qua phiên bản của thông báo cập nhật, phiên bản mới hơn sẽ tiếp tục được thông báo chỉ khi chúng lớn hơn giá trị này.                                          |
| EnableBannerAndPost             | bool    | -                | Hiển thị thông báo trò chơi ở trong trình khởi chạy.                                                                                                         |
| IgnoreRunningGame               | bool    | -                | Bỏ qua trò chơi đang chạy, trang trình khởi chạy sẽ không còn hiển thị `trò chơi đang chạy` khi được bật.                                                         |
| SelectGameBiz                   | GameBiz | -                | Khu vực trò chơi được chọn cuối cùng.                                                                                                                             |
| ShowNoviceGacha                 | bool    | -                | Hiển thị số liệu gacha người mới.                                                                                                                                 |
| GachaLanguage                   | string? | -                | Nhận ngôn ngữ được sử dụng cho lịch sử gacha, mặc định là ngôn ngữ trong trò chơi.                                                                                |
| EnableDynamicAccentColor        | bool    | -                | Màu chủ đề động được lấy từ ảnh nền, và màu chủ đề hệ thống được sử dụng khi tắt.                                                                                 |
| AccentColor                     | string? | -                | Màu chủ đề động được lưu trong bộ nhớ cache, được sử dụng để giảm số lượng tính toán khi khởi động, `#ARBG#ARBG`: màu trước là màu nền và màu sau là màu văn bản. |
| VideoBgVolume                   | int     | 100              | Âm lượng của video nền, `0 - 100`.                                                                                                                                |
| PauseVideoWhenChangeToOtherPage | bool    | -                | **Đã lỗi thời:** Tạm dừng video khi chuyển sang trang không có trình khởi chạy.                                                                                       |
| UseOneBg                        | bool    | -                | Sử dụng cùng một hình nền cho tất cả các khu vực trò chơi, thường được bật khi sử dụng nền video.                                                                 |
| AcceptHoyolabToolboxAgreement   | bool    | -                | Chấp nhận tuyên bố từ chối trách nhiệm của trang công cụ HoYoLAB.                                                                                                 |
| HoyolabToolboxPaneOpen          | bool    | true             | Thanh bên điều hướng ở trang công cụ HoYoLAB có mở hay không.                                                                                                     |

## Cài đặt động (Dynamic Settings)

Các mục cài đặt động có các giá trị khác nhau trong từng vùng trò chơi, các key cài đặt của chúng sẽ có vùng trò chơi được thêm vào cuối, ví dụ, mục cài đặt `custom_bg`, có key của Genshin Impact (Toàn cầu) is `custom_gb_hk4e_global`.

| Key                          | Kiểu    | Giá trị mặc định | Chú thích                                                                                                                              |
| ---------------------------- | ------- | ---------------- | -------------------------------------------------------------------------------------------------------------------------------------- |
| bg                           | string? | -                | Tên tập tin hình nền chính thức, tập tin nằm trong thư mục con `bg` của thư mục dữ liệu người dùng.                                    |
| custom_bg                    | string? | -                | Hình nền tùy chỉnh, hình ảnh là đường dẫn đầy đủ của tên tập tin và video.                                                             |
| enable_custom_bg             | bool    | -                | Có bật hình nền tuỳ chỉnh hay không.                                                                                                   |
| install_path                 | string? | -                | Thư mục cài đặt trò chơi, không phải thư mục của trình khởi chạy chính thức.                                                               |
| enable_third_party_tool      | bool    | -                | Có bật công cụ của bên thứ ba để bắt đầu trò chơi hay không.                                                                           |
| third_party_tool_path        | string? | -                | Đường dẫn đến tập tin của công cụ bên thứ ba.                                                                                          |
| start_argument               | string? | -                | Đối số khởi động trò chơi                                                                                                              |
| last_gacha_uid               | long    | -                | UID được chọn cuối cùng trong trang bản ghi gacha.                                                                                     |
| last_region_of               | GameBiz | -                | Khu vực trò chơi được chọn cuối cùng, được sử dụng để chuyển đổi nhanh ở đầu ứng dụng, với tên đầy đủ của trò chơi được thêm vào cuối. |
| last_select_game_record_role | long    | -                | UID được chọn cuối cùng của vai trò trò chơi trong trang hộp công cụ HoYoLAB.                                                          |
