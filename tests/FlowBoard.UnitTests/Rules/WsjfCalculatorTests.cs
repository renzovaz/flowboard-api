using FlowBoard.Api.Domain.Exceptions;
using FlowBoard.Api.Domain.Rules;

namespace FlowBoard.UnitTests.Rules;

/// <summary>R7 — Cálculo e ordenação por WSJF.</summary>
public class WsjfCalculatorTests
{
    // U18
    [Fact]
    public void Calculate_ReturnsCostOfDelayDividedByJobSize()
    {
        // (13 + 8 + 2) / 3 = 7,666... → 7,67
        var wsjf = WsjfCalculator.Calculate(13, 8, 2, 3);

        Assert.Equal(23, WsjfCalculator.CostOfDelay(13, 8, 2));
        Assert.Equal(7.67m, WsjfCalculator.Round(wsjf));
    }

    // U19
    [Fact]
    public void Order_TieBreaksByCostOfDelayThenCreatedAtThenId()
    {
        var t0 = TestData.Now;
        var items = new[]
        {
            new Item(Id: 1, CostOfDelay: 8, Wsjf: 1.00m, CreatedAt: t0),
            new Item(Id: 2, CostOfDelay: 16, Wsjf: 2.00m, CreatedAt: t0.AddMinutes(5)), // mesmo WSJF, CoD maior
            new Item(Id: 3, CostOfDelay: 8, Wsjf: 2.00m, CreatedAt: t0.AddMinutes(1)),  // mesmo WSJF e CoD, mais antiga
            new Item(Id: 5, CostOfDelay: 8, Wsjf: 2.00m, CreatedAt: t0.AddMinutes(2)),  // empate total com o Id 4
            new Item(Id: 4, CostOfDelay: 8, Wsjf: 2.00m, CreatedAt: t0.AddMinutes(2)),
            new Item(Id: 6, CostOfDelay: 23, Wsjf: 7.67m, CreatedAt: t0.AddMinutes(9)),
        };

        var ordered = WsjfCalculator.Order(items).Select(i => i.Id);

        Assert.Equal([6, 2, 3, 4, 5, 1], ordered);
    }

    // U20
    [Theory]
    [InlineData(4)]
    [InlineData(21)]
    [InlineData(0)]
    public void ValidateScale_ValueOutsideScale_ThrowsInvalidWsjfValue(int value)
    {
        var ex = Assert.Throws<BusinessRuleException>(() => WsjfCalculator.ValidateScale(value, "jobSize"));

        Assert.Equal(ErrorCodes.InvalidWsjfValue, ex.ErrorCode);
    }

    private sealed record Item(int Id, int CostOfDelay, decimal Wsjf, DateTime CreatedAt) : IWsjfItem;
}
