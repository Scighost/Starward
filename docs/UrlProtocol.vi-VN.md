> Phiên bản của bài viết này là **v1**, nếu phiên bản bị tụt lại phía sau, vui lòng chọn [Original](../urlProtocol.md).

# Giao thức URL

Các phần mềm khác thậm chí cả trang web cũng có thể sử dụng giao thức url `starward://` để gọi một số tính năng của Starward. Giao thức URL chỉ được đăng ký khi người dùng bật tính năng này trong trang cài đặt.

![URL Protocol](https://user-images.githubusercontent.com/88989555/278791338-1b516f5d-95dd-42a1-b620-ec0bc9e2f421.png)

## Các tính năng có sẵn

Tham số `game_biz` sau đây là mã nhận dạng khu vực trò chơi và có thể được xem trong [docs/Configuration.vi-VN.md](./docs/Configuration.vi-VN.md#game-regions) .

**Các truy vấn được chấp nhận**

| Khoá         | Kiểu dữ liệu | Mô tả                                             |
| ------------ | ------------ | ------------------------------------------------- |
| uid          | `number`     | Chuyển sang tài khoản cụ thể trước khi khởi động. |
| install_path | `string`     | Thư mục đầy đủ của tệp thực thi trò chơi.         |

### Bắt đầu trò chơi

```
starward://startgame/{game_biz}
```

### Ghi lại thời gian chơi

```
starward://playtime/{game_biz}
```

### Kiểm tra giao thức URL

```
starward://test/
```
Sử dụng tham số url này để bật lên cửa sổ kiểm tra giao thức url
