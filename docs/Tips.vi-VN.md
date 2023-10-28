# Mẹo

Tài liệu này chứa các tính năng ẩn có thể không được phát hiện trong quá trình sử dụng bình thường.

## Nhanh chóng trở về trang trình khởi chạy

Nhấn đúp chuột vào biểu tượng trò chơi trên thanh tiêu đề.

## Chuyển đổi client trò chơi

Hầu hết các tệp giống nhau giữa các client trên các máy chủ khác nhau cho mỗi trò chơi, chỉ có một số điểm khác biệt. Tính năng này thuận tiện cho người dùng nhanh chóng chuyển đổi client sang máy chủ trò chơi khác.

> Honkai: Star Rail không được hỗ trợ.

### Các bước sử dụng

- Cài đặt trò chơi bất kì
- Chuyển sang máy chủ khác của trò chơi ở thanh tiêu đề
- Nhấn vào vào nút cài đặt trong trang trình khởi chạy
- Xác định vị trí thư mục cài đặt trò chơi
- Nhấn **Sửa chữa trò chơi**
- Đợi quá trình xác minh và tải xuống hoàn tất

<img src="https://user-images.githubusercontent.com/61003590/259013561-907934e2-29fd-46ee-8e1c-83cb1daaa143.png" width="800px" />

## Chuyển đổi ngôn ngữ của bản ghi gacha

Theo mặc định, ngôn ngữ của vật phẩm gacha sẽ được chọn ở trong trò chơi, nhưng nó có thể thay đổi thủ công ở menu cài đặt ở trang bản ghi gacha. Bạn có thể điền vào tất cả các danh mục ngôn ngữ được trò chơi hỗ trợ. Nếu bạn không nhấn vào **Áp dụng**, bản ghi gacha mới sẽ sử dụng cài đặt ngôn ngữ này; nếu bạn nhấp vào **Áp dụng**, tất cả các bản ghi gacha hiện tại sẽ được chuyển đổi sang ngôn ngữ đã đặt.

<img src="https://user-images.githubusercontent.com/88989555/259012116-d84e7feb-9949-454c-9c46-c9bc77c1ea3a.png" width="200px" />

## Thiêt lập mở rộng

Kể từ phiên bản [0.10.2](https://github.com/Scighost/Starward/releases/tag/0.10.2), Starward đã thêm một số cài đặt tiện ích mở rộng để bật hoặc tắt một số tính năng. Các cài đặt này không có trang chỉnh sửa trong ứng dụng và bạn cần sửa đổi chúng trong tệp cấu hình Starward `config.ini`.

Cài đặt tồn tại theo dạng cặp khóa-giá trị. Ví dụ: nếu bạn muốn tắt lời nhắc chấm đỏ về thông báo trò chơi, bạn cần thêm 'DisableGameNoticeRedHot = True' vào tệp `config.ini`.

| Khóa                       | Giá trị khả dụng  | Mô tả                                       |
| -------------------------- | ----------------- | ------------------------------------------- |
| DisableNavigationShortcut  | `True` \| `False` | Ẩn phím tắt điều hướng.                     |
| DisableGameNoticeRedHot    | `True` \| `False` | Tắt nhắc nhở chấm đỏ về thông báo trò chơi. |
| DisableGameAccountSwitcher | `True` \| `False` | Ẩn trình chuyển đổi tài khoản trò chơi.     |
