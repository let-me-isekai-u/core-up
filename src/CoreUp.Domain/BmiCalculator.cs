namespace CoreUp.Domain;

public static class BmiCalculator
{
    public static decimal Calculate(decimal heightCm, decimal weightKg)
    {
        if (heightCm <= 0) throw new ArgumentOutOfRangeException(nameof(heightCm));
        if (weightKg <= 0) throw new ArgumentOutOfRangeException(nameof(weightKg));

        var heightM = heightCm / 100m;
        return Math.Round(weightKg / (heightM * heightM), 2, MidpointRounding.AwayFromZero);
    }

    public static BmiCategory Classify(decimal bmi) => bmi switch
    {
        < 18.5m => BmiCategory.Underweight,
        < 25m => BmiCategory.Normal,
        < 30m => BmiCategory.Overweight,
        _ => BmiCategory.Obese
    };
}
