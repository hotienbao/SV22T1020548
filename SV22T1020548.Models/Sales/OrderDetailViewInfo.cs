namespace SV22T1020548.Models.Sales
{
    public class OrderDetailViewInfo : OrderDetail
    {
        // 🔥 FIX: dùng new để tránh warning
        public new string ProductName { get; set; } = "";

        public string Unit { get; set; } = "";

        public string Photo { get; set; } = "";

        public decimal TotalPrice => Quantity * SalePrice;
    }
}