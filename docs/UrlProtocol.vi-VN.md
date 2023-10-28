# Giao thức URL

Các phần mềm khác thậm chí cả trang web cũng có thể sử dụng giao thức url `starward://` để gọi một số tính năng của Starward. Giao thức URL chỉ được đăng ký khi người dùng bật tính năng này trong trang cài đặt.

![URL Protocol](https://github.com/phucho0237/Starward/assets/88989555/eb454803-b1f9-468f-94ea-3a431e04457f)

## Các tính năng có sẵn

Tham số `game_biz` sau đây là mã nhận dạng khu vực trò chơi và có thể được xem trong [docs/Configuration.vi-VN.md](./docs/Configuration.vi-VN.md#game-regions) .

### Bắt đầu trò chơi

```
starward://startgame/{game_biz}
```

**Các truy vấn được chấp nhận**

| Khoá         | Kiểu dữ liệu | Mô tả                                             |
| ------------ | ------------ | ------------------------------------------------- |
| uid          | `number`     | Chuyển sang tài khoản cụ thể trước khi khởi động. |
| install_path | `string`     | Thư mục đầy đủ của tệp thực thi trò chơi.         |

### Ghi lại thời gian chơi

```
starward://playtime/{game_biz}
```
