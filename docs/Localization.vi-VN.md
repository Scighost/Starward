> Phiên bản của tài liệu này là **v1**. Nếu phiên bản chưa được cập nhật, vui lòng tham khảo [tài liệu gốc](./Localization.md).

# Dịch thuật

Đầu tiên, tôi xin gửi lời cảm ơn chân thành nhất đến tất cả những người đóng góp cho dự án này. Nhờ những đóng góp quên mình của bạn, Starward có thể được sử dụng bởi mọi người ở nhiều ngôn ngữ khác nhau trên thế giới. Cho dù đóng góp của bạn là một dòng mã, một bản sửa lỗi hay một đề xuất thì công việc của bạn sẽ tăng thêm giá trị đáng kể cho dự án. Mọi người đều là một phần không thể thiếu của cộng đồng sôi động này.

<picture>
    <source srcset="https://github.com/Scighost/Starward/assets/61003590/9d369ec3-ab7c-408f-88c2-11bfe4453208" type="image/avif" />
    <img src="https://github.com/Scighost/Starward/assets/61003590/44552992-e2c5-451f-9c2a-73176e8e4e93" width="240px" />
</picture>


## Hướng dẫn dịch thuật

Nếu bạn muốn đóng góp vào nỗ lực dịch thuật của dự án này, vui lòng đọc thông tin sau. Có hai phần của dự án cần dịch: tài liệu được lưu trữ ở GitHub và văn bản trong ứng dụng.


## Dịch tài liệu

Các tài liệu được đề cập trong bài viết này là các file Markdown trong repo. Không phải tất cả các tài liệu đều cần phải dịch. Tiêu đề của tài liệu cần dịch sẽ có liên kết tới các phiên bản ngôn ngữ khác. Ví dụ:

**v1** | English | [简体中文](./Localization.zh-CN.md)

Trước khi bạn bắt đầu dịch, vui lòng xóa các liên kết tiêu đề trỏ đến các phiên bản bằng ngôn ngữ khác khỏi tài liệu gốc. Sau đó, thêm nội dung sau. Số phiên bản là nội dung được in đậm trong ví dụ trên và trong ngoặc là đường dẫn tương đối đến tài liệu nguồn bạn đang dịch.

> Phiên bản của tài liệu này là v1. Nếu phiên bản chưa được cập nhật, vui lòng tham khảo [tài liệu gốc](./Localization.md).

Một số tài liệu có thể bao gồm hình ảnh. Để giữ cho kích thước kho lưu trữ có thể quản lý được, số lượng lớn hình ảnh vào kho lưu trữ của dự án này không được phép. Nếu bạn muốn bản địa hóa những hình ảnh này, bạn có thể tạo một [issue](https://github.com/Scighost/Starward/issues), tải hình ảnh lên rồi thay thế các liên kết trong tài liệu.

Sau khi dịch xong, vui lòng gửi lại về repo này thông qua Pull Request.


## Dịch văn bản trong ứng dụng

Bản dịch văn bản trong ứng dụng cho Starward được lưu trữ trên nền tảng [Crowdin](https://crowdin.com/project/starward), nơi bạn có thể sửa đổi nội dung văn bản bất cứ lúc nào. Nếu bạn muốn thêm ngôn ngữ dịch mới, vui lòng tạo [issue](https://github.com/Scighost/Starward/issues).

Những thay đổi bạn thực hiện trên Crowdin sẽ được đồng bộ hóa với nhánh [l10n/main](https://github.com/Scighost/Starward/tree/l10n/main) trong vòng 1 giờ đổng hồ, kích hoạt quá trình build tự động. Tìm quy trình làm việc mới nhất có tên `New Crowdin updates` trong [GitHub Actions](https://github.com/Scighost/Starward/actions/workflows/build.yml) và tải xuống tệp đã biên dịch (Artifacts). Bạn có thể kiểm tra hiệu ứng hiển thị của văn bản dịch trong ứng dụng theo thời gian thực. **Phiên bản đang được phát triển có thể làm hỏng cơ sở dữ liệu cá nhân của bạn `StarwardDatabase.db`. Vui lòng sao lưu trước khi kiểm tra. Phiên bản này không nên được sử dụng trong một thời gian dài.**

Vì trình độ tiếng Anh của tôi còn hạn chế nên có thể mắc nhiều lỗi khác nhau. Vì văn bản nguồn không thể được sửa đổi tự do trên Crowdin nên nếu bạn thấy bất kỳ khu vực nào trong văn bản tiếng Anh cần chỉnh sửa, vui lòng gửi các sửa đổi của bạn thông qua Pull Request.
