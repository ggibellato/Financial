using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Domain.Rules;

public abstract class ValuationRules
{
    public static readonly ValuationRules PerUnit = new PerUnitRules();
    public static readonly ValuationRules PositionTotal = new PositionTotalRules();

    public static ValuationRules For(ValuationMethod method) =>
        method is ValuationMethod.ProviderValue or ValuationMethod.Manual ? PositionTotal : PerUnit;

    public abstract bool IsBalance { get; }

    public abstract bool AllowsZeroPrice { get; }

    public abstract bool FetchesAutomatically { get; }

    public abstract PriceSource ManualEntrySource { get; }

    public abstract decimal ValueAt(decimal quantity, AssetPriceSnapshot price);

    public abstract decimal ValueOfPortion(decimal shares, decimal quantityHeld, AssetPriceSnapshot price);

    public abstract decimal? ValueWithoutPrice(decimal quantity);

    private sealed class PerUnitRules : ValuationRules
    {
        public override bool IsBalance => false;

        public override bool AllowsZeroPrice => false;

        public override bool FetchesAutomatically => true;

        public override PriceSource ManualEntrySource => PriceSource.Manual;

        public override decimal ValueAt(decimal quantity, AssetPriceSnapshot price) => quantity * price.Price;

        public override decimal ValueOfPortion(decimal shares, decimal quantityHeld, AssetPriceSnapshot price) => shares * price.Price;

        public override decimal? ValueWithoutPrice(decimal quantity) => quantity == 0m ? 0m : null;
    }

    private sealed class PositionTotalRules : ValuationRules
    {
        public override bool IsBalance => true;

        public override bool AllowsZeroPrice => true;

        public override bool FetchesAutomatically => false;

        public override PriceSource ManualEntrySource => PriceSource.ProviderValuation;

        public override decimal ValueAt(decimal quantity, AssetPriceSnapshot price) => price.Price;

        public override decimal ValueOfPortion(decimal shares, decimal quantityHeld, AssetPriceSnapshot price) =>
            price.Price * (shares / quantityHeld);

        public override decimal? ValueWithoutPrice(decimal quantity) => null;
    }
}
