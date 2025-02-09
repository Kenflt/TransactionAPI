namespace TransactionAPI.Models
{
    public class ItemDetail
    {
        public string PartnerItemRef { get; set; }
        public string Name { get; set; }
        public int Qty { get; set; }
        public long UnitPrice { get; set; }
    }
}