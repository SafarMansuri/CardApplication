namespace CardApplicationAPI.Models
{
    public class BankTransferResult
    {
        public BankTransaction SourceTransaction { get; set; } = new();
        public BankTransaction DestinationTransaction { get; set; } = new();
    }
}