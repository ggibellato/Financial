
using System;

namespace FinancialModel.Model
{
    public class Operation
    {
        public enum OperationType { Buy, Sell }
        public DateTime Date { get; set; }
        public OperationType Type { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Fees { get; set; }
    }
}