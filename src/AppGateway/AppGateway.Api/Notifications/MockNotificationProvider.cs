using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AppGateway.Contracts.Notifications;

namespace AppGateway.Api.Notifications;

internal sealed class MockNotificationProvider : INotificationProvider
{
    public Task<IReadOnlyCollection<NotificationItemDto>> GetNotificationsAsync(
        string? userObjectId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        IReadOnlyCollection<NotificationItemDto> notifications =
        [
            new("notif-1", "Tài liệu Q2 Financial Report đã được cập nhật", "Alex Chen đã tải lên phiên bản mới của báo cáo tài chính.", "system", now.AddMinutes(-15), false, "/documents/q2-financial-report"),
            new("notif-2", "Cuộc họp đánh giá thiết kế bắt đầu sau 30 phút", "Phòng Product Design tổ chức họp review sprint tuần này.", "event", now.AddMinutes(-45), false, "/calendar/design-review"),
            new("notif-3", "Nhắc việc hoàn tất phê duyệt hợp đồng", "Bạn còn một bước duyệt cuối cùng cho hợp đồng đối tác ACME.", "reminder", now.AddHours(-3), true, "/approvals/acme-contract"),
            new("notif-4", "Hệ thống cảnh báo bảo mật", "Đăng nhập bất thường được phát hiện từ IP ở Singapore.", "alert", now.AddHours(-6), false, null),
            new("notif-5", "Giao nhiệm vụ mới: Chiến dịch Q3", "Hoàn thiện nội dung Landing Page cho chiến dịch marketing Q3.", "task", now.AddHours(-26), true, "/projects/q3-campaign"),
            new("notif-6", "Thông báo hệ thống: Bảo trì định kỳ", "Hệ thống sẽ bảo trì từ 22:00 đến 23:00 tối nay. Vui lòng hoàn tất công việc trước thời gian này.", "system", now.AddHours(-28), false, null),
            new("notif-7", "Nhắc việc: Hoàn tất báo cáo tuần", "Hạn chót gửi báo cáo tuần là 17:00 hôm nay.", "reminder", now.AddHours(-30), false, null),
            new("notif-8", "Đã hoàn tất phê duyệt hợp đồng", "Hợp đồng đối tác SunTech đã được phê duyệt bởi phòng Pháp chế.", "task", now.AddHours(-36), true, "/contracts/suntech"),
            new("notif-9", "Cảnh báo bảo mật", "Phát hiện đăng nhập thất bại nhiều lần từ tài khoản của bạn.", "alert", now.AddHours(-40), false, null),
            new("notif-10", "Sự kiện: Workshop Thiết kế trải nghiệm người dùng", "Tham gia workshop cùng chuyên gia UX từ Google vào thứ Sáu tuần này.", "event", now.AddHours(-48), true, "/events/ux-workshop"),
            new("notif-11", "Tin mới: Bộ tài liệu hướng dẫn sản phẩm", "Đã phát hành bộ tài liệu hướng dẫn sử dụng sản phẩm phiên bản 2.0.", "system", now.AddHours(-52), true, "/knowledge-base/product-guide"),
        ];

        return Task.FromResult(notifications);
    }
}
