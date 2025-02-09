using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TransactionAPI.Models;

namespace TransactionAPI.Services
{
    public class TransactionService : ITransactionService
    {
        private static readonly Dictionary<string, string> AllowedPartners = new()
        {
            { "FAKEGOOGLE", "FAKEPASSWORD1234" },
            { "FAKEPEOPLE", "FAKEPASSWORD4578" }
        };

        public virtual TransactionResponse ProcessTransaction(TransactionRequest request)
        {
            string missingParam = ValidateRequiredFields(request);
            if (missingParam != null)
                return new TransactionResponse { Result = 0, ResultMessage = missingParam + " is Required." };

            if (!IsValidPartner(request.PartnerKey, request.PartnerPassword))
                return new TransactionResponse { Result = 0, ResultMessage = "Access Denied!" };

            if (request.Timestamp == null || IsExpired(request.Timestamp))
                return new TransactionResponse { Result = 0, ResultMessage = "Expired." };

            if (!IsValidPartnerRefNo(request.PartnerRefNo))
                return new TransactionResponse { Result = 0, ResultMessage = "Invalid Partner Reference Number Format." };

            if (request.TotalAmount > 0 && (request.Items == null || request.Items.Count == 0))
                return new TransactionResponse { Result = 0, ResultMessage = "Items List Cannot Be Empty." };

            foreach (var item in request.Items)
            {
                if (string.IsNullOrEmpty(item.PartnerItemRef))
                    return new TransactionResponse { Result = 0, ResultMessage = "PartnerItemRef is Required." };
                if (string.IsNullOrEmpty(item.Name))
                    return new TransactionResponse { Result = 0, ResultMessage = "Item Name is Required." };
                if (item.Qty < 1 || item.Qty > 5)
                    return new TransactionResponse { Result = 0, ResultMessage = "Quantity must be between 1 and 5." };
                if (item.UnitPrice <= 0)
                    return new TransactionResponse { Result = 0, ResultMessage = "UnitPrice must be a positive value." };
            }

            long calculatedTotal = request.Items.Sum(item => item.Qty * item.UnitPrice);

            if (calculatedTotal != request.TotalAmount)
                return new TransactionResponse { Result = 0, ResultMessage = "Invalid Total Amount." };

            double totalAmountMYR = request.TotalAmount / 100.0;
            long baseDiscount = (long)(CalculateBaseDiscount(totalAmountMYR) * 100);
            long conditionalDiscount = (long)(CalculateConditionalDiscount(totalAmountMYR) * 100);
            long maxAllowedDiscount = (long)(request.TotalAmount * 0.2);
            long totalDiscount = Math.Min(baseDiscount + conditionalDiscount, maxAllowedDiscount);
            long finalAmount = request.TotalAmount - totalDiscount;

            return new TransactionResponse
            {
                Result = 1,
                TotalAmount = request.TotalAmount,
                TotalDiscount = totalDiscount,
                FinalAmount = finalAmount,
                ResultMessage = "Transaction Successful"
            };
        }

        private string ValidateRequiredFields(TransactionRequest request)
        {
            if (string.IsNullOrEmpty(request.PartnerKey)) return "partnerkey";
            if (string.IsNullOrEmpty(request.PartnerPassword)) return "partnerpassword";
            if (string.IsNullOrEmpty(request.PartnerRefNo)) return "partnerrefno";
            if (string.IsNullOrEmpty(request.Timestamp)) return "timestamp";
            if (string.IsNullOrEmpty(request.Sig)) return "sig";
            return null;
        }

        private bool IsValidPartner(string partnerKey, string encodedPassword)
        {
            if (!AllowedPartners.TryGetValue(partnerKey, out string expectedPassword))
                return false;

            string decodedPassword = Encoding.UTF8.GetString(Convert.FromBase64String(encodedPassword));
            return expectedPassword == decodedPassword;
        }

        private bool IsExpired(string timestamp)
        {
            if (!DateTime.TryParse(timestamp, out DateTime requestTime))
                return true;

            return Math.Abs((DateTime.UtcNow - requestTime).TotalSeconds) > 300;
        }

        private bool IsValidPartnerRefNo(string partnerRefNo)
        {
            return !string.IsNullOrEmpty(partnerRefNo) && System.Text.RegularExpressions.Regex.IsMatch(partnerRefNo, "^[a-zA-Z0-9-]{6,}$");
        }

        private double CalculateBaseDiscount(double totalAmountMYR)
        {
            if (totalAmountMYR < 200) return 0;
            if (totalAmountMYR <= 500) return totalAmountMYR * 0.05;
            if (totalAmountMYR <= 800) return totalAmountMYR * 0.07;
            if (totalAmountMYR <= 1200) return totalAmountMYR * 0.10;
            return totalAmountMYR * 0.15;
        }

        private double CalculateConditionalDiscount(double totalAmountMYR)
        {
            double discount = 0;
            if (IsPrime((long)totalAmountMYR) && totalAmountMYR > 500)
                discount += totalAmountMYR * 0.08;
            if (totalAmountMYR > 900 && totalAmountMYR % 10 == 5)
                discount += totalAmountMYR * 0.10;
            return discount;
        }

        private bool IsPrime(long num)
        {
            if (num < 2) return false;
            for (long i = 2; i * i <= num; i++)
            {
                if (num % i == 0) return false;
            }
            return true;
        }
    }
}
