using System;

namespace Financial.CashFlow.Domain.Entities
{
    public class CreditCard
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public bool IsActive { get; private set; }
        public DateOnly? NextInvoiceDueDate { get; private set; }

        private CreditCard() { }

        public static CreditCard Create(string name, bool isActive = true)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Credit card name is required.");
            }

            return new CreditCard
            {
                Id = Guid.NewGuid(),
                Name = name,
                IsActive = isActive
            };
        }

        public void UpdateDetails(DateOnly? nextInvoiceDueDate, bool isActive)
        {
            NextInvoiceDueDate = nextInvoiceDueDate;
            IsActive = isActive;
        }
    }
}
