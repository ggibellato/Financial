using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Domain.Rules;

public static class ArcaRules
{
    public static bool CountsTowardArca(this ArcaBucket? bucket) => bucket is not null and not ArcaBucket.ReserveFund;

    public static int DisplayOrder(this ArcaBucket? bucket) => bucket switch
    {
        ArcaBucket.Shares => 0,
        ArcaBucket.RealEstate => 1,
        ArcaBucket.Cash => 2,
        ArcaBucket.Pension => 3,
        ArcaBucket.ReserveFund => 4,
        _ => 5,
    };
}
