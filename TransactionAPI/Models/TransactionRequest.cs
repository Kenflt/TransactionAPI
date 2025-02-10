using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TransactionAPI.Models
{
    public class TransactionRequest
    {
        [StringLength(50)]
        public string PartnerKey { get; set; }

        [StringLength(50)]
        public string PartnerRefNo { get; set; }

        [StringLength(50)]
        public string PartnerPassword { get; set; }
        public long TotalAmount { get; set; }
        public List<ItemDetail> Items { get; set; }
        public string Timestamp { get; set; }
        public string Sig { get; set; }
    }
}