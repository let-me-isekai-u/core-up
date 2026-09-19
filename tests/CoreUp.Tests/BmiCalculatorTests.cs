using CoreUp.Domain;

namespace CoreUp.Tests;

public sealed class BmiCalculatorTests
{
    [Fact]
    public void Calculate_82KgAnd170Cm_ReturnsOverweightBmi()
    {
        var bmi = BmiCalculator.Calculate(170m, 82m);
        Assert.Equal(28.37m, bmi);
        Assert.Equal(BmiCategory.Overweight, BmiCalculator.Classify(bmi));
    }

    [Theory]
    [InlineData(18.49, BmiCategory.Underweight)]
    [InlineData(18.5, BmiCategory.Normal)]
    [InlineData(24.99, BmiCategory.Normal)]
    [InlineData(25, BmiCategory.Overweight)]
    [InlineData(29.99, BmiCategory.Overweight)]
    [InlineData(30, BmiCategory.Obese)]
    public void Classify_UsesExpectedBoundaries(double value, BmiCategory expected) =>
        Assert.Equal(expected, BmiCalculator.Classify((decimal)value));
}
