using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Infrastructure.Persistence;

public sealed class ReserveBucketReferenceConverter(Dictionary<Guid, ReserveBucket>? lookup)
    : ReferenceConverter<ReserveBucket>(lookup, bucket => bucket.Id, "ReserveBucket");
