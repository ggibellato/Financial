using System;

namespace FinancialModel.Model
{
    public class Credit
    {
        public enum CreditType { Dividend, Rent }

        public DateTime Date { get; set; }
        public CreditType Type { get; set; }
        public decimal Value { get; set; }
    }
}
