namespace SV22T1020548.Models.Sales
{
    /// <summary>
    /// DTO mở rộng để map thêm trường RejectNote (nếu cột tồn tại trong bảng Orders).
    /// </summary>
    public class OrderViewInfoWithRejectNote : OrderViewInfo
    {
        public string? RejectNote { get; set; }
    }
}
