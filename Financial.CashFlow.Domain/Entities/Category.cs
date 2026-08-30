using System;

namespace Financial.CashFlow.Domain.Entities
{
    public class Category
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public bool Active { get; private set; }
        public bool IsInvestment { get; private set; }
        public bool IsTithe { get; private set; }

        private Category() { }

        public static Category Create(string name, bool isInvestment = false, bool isTithe = false, bool isActive = true)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Category name is required.");
            }

            return new Category
            {
                Id = Guid.NewGuid(),
                Name = name,
                Active = isActive,
                IsInvestment = isInvestment,
                IsTithe = isTithe
            };
        }

        /// <summary>Updates this category's fields. Callers own uniqueness checks, since only the
        /// repository can see across every category.</summary>
        public void Update(string name, bool active, bool isInvestment, bool isTithe)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Category name is required.");
            }

            Name = name;
            Active = active;
            IsInvestment = isInvestment;
            IsTithe = isTithe;
        }
    }
}
