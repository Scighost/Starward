# Giao thức URL

Các phần mềm khác thậm chí cả trang web cũng có thể sử dụng giao thức url `starward://` để gọi một số tính năng của Starward. Giao thức URL chỉ được đăng ký khi người dùng bật tính năng này trong trang cài đặt.


![URL Protocol](https://user-images.githubusercontent.com/61003590/278273851-7c614cde-d8c4-403b-876e-cecc3570f684.png)


## Các tính năg có sẵn

Tham số `game_biz` sau đây là mã nhận dạng khu vực trò chơi và có thể được xem trong [docs/Configuration.md](./docs/Configuration.vi-VN.md#game-regions) .

### Bắt đầu trò chơi

```
starward://startgame/{game_biz}
```

**Các truy vấn được chấp nhận**

|Khoá|Kiểu dữ liệu|Mô tả|
|---|---|---|
|uid| `number` | Switch to specific account before startup. |
|install_path| `string` | Full folder of game executable. |


### Ghi lại thời gian chơi

```
starward://playtime/{game_biz}
```
