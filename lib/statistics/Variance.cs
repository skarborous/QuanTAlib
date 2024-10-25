namespace QuanTAlib;

/// <summary>
/// Represents a variance calculator that measures the spread of a set of numbers
/// from their average value.
/// </summary>
/// <remarks>
/// The Variance class calculates either the population variance or the sample
/// variance based on the isPopulation parameter. It uses a circular buffer
/// to efficiently manage the data points within the specified period.
///
/// In financial analysis, variance is important for:
/// - Measuring the dispersion of returns around the mean.
/// - Assessing risk and volatility in financial instruments or portfolios.
/// - Serving as a basis for other risk measures like standard deviation and beta.
/// - Contributing to portfolio optimization techniques, such as Modern Portfolio Theory.
/// </remarks>
public class Variance : AbstractBase
{
    /// <summary>
    /// Indicates whether to calculate population (true) or sample (false) variance.
    /// </summary>
    private readonly bool IsPopulation;

    /// <summary>
    /// Circular buffer to store the most recent data points.
    /// </summary>
    private readonly CircularBuffer _buffer;

    /// <summary>
    /// Initializes a new instance of the Variance class with the specified period and
    /// population flag.
    /// </summary>
    /// <param name="period">The period over which to calculate the variance.</param>
    /// <param name="isPopulation">
    /// A flag indicating whether to calculate population (true) or sample (false) variance.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when period is less than 2.
    /// </exception>
    public Variance(int period, bool isPopulation = false)
    {
        if (period < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(period), "Period must be greater than or equal to 2.");
        }
        IsPopulation = isPopulation;
        WarmupPeriod = 0;
        _buffer = new CircularBuffer(period);
        Name = $"Variance(period={period}, population={isPopulation})";
        Init();
    }

    /// <summary>
    /// Initializes a new instance of the Variance class with the specified source, period,
    /// and population flag.
    /// </summary>
    /// <param name="source">The source object to subscribe to for value updates.</param>
    /// <param name="period">The period over which to calculate the variance.</param>
    /// <param name="isPopulation">
    /// A flag indicating whether to calculate population (true) or sample (false) variance.
    /// </param>
    public Variance(object source, int period, bool isPopulation = false) : this(period, isPopulation)
    {
        var pubEvent = source.GetType().GetEvent("Pub");
        pubEvent?.AddEventHandler(source, new ValueSignal(Sub));
    }

    /// <summary>
    /// Initializes the Variance instance by clearing the buffer.
    /// </summary>
    public override void Init()
    {
        base.Init();
        _buffer.Clear();
    }

    /// <summary>
    /// Manages the state of the Variance instance based on whether a new value is being processed.
    /// </summary>
    /// <param name="isNew">Indicates whether the current input is a new value.</param>
    protected override void ManageState(bool isNew)
    {
        if (isNew)
        {
            _lastValidValue = Input.Value;
            _index++;
        }
    }

    /// <summary>
    /// Performs the variance calculation for the current period.
    /// </summary>
    /// <returns>
    /// The calculated variance value for the current period.
    /// </returns>
    /// <remarks>
    /// This method calculates the variance using the formula:
    /// sum((x - mean)^2) / n for population, or
    /// sum((x - mean)^2) / (n - 1) for sample,
    /// where x is each value, mean is the average of all values, and n is the number of values.
    /// If there's only one value in the buffer, the method returns 0.
    ///
    /// Interpretation of results:
    /// - A low variance indicates that the values tend to be close to the mean and to each other.
    /// - A high variance indicates that the values are spread out over a wider range.
    /// - In financial contexts, higher variance often implies higher volatility or risk.
    /// - Variance is always non-negative, and its units are squared units of the original data.
    /// </remarks>
    protected override double Calculation()
    {
        ManageState(Input.IsNew);

        _buffer.Add(Input.Value, Input.IsNew);

        double variance = 0;
        if (_buffer.Count > 1)
        {
            var values = _buffer.GetSpan().ToArray();
            double mean = values.Average();
            double sumOfSquaredDifferences = values.Sum(x => Math.Pow(x - mean, 2));

            double divisor = IsPopulation ? _buffer.Count : _buffer.Count - 1;
            variance = sumOfSquaredDifferences / divisor;
        }

        IsHot = true;
        return variance;
    }
}
