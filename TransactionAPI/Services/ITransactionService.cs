using TransactionAPI.Models;

namespace TransactionAPI.Services
{
    public interface ITransactionService
    {
        TransactionResponse ProcessTransaction(TransactionRequest request);
    }
}
