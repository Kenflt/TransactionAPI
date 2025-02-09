using System.ComponentModel.DataAnnotations;

namespace TransactionAPI.Models
{
    public class ItemDetail
    {
        [Required]
        [StringLength(50, ErrorMessage = "PartnerItemRef cannot exceed 50 characters.")]
        public string PartnerItemRef { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Quantity must be between 1 and 5.")]
        public int Qty { get; set; }

        [Required]
        [Range(1, long.MaxValue, ErrorMessage = "UnitPrice must be a positive value.")]
        public long UnitPrice { get; set; }
    }

}