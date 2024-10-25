namespace QuanTAlib;

/// <summary>
/// Calculates the median value over a specified period.
/// Provides a measure of central tendency that is robust to outliers.
/// </summary>
/// <remarks>
/// The Median indicator is particularly useful in financial analysis for:
/// - Providing a robust measure of central tendency that is less affected by extreme values than the mean.
/// - Identifying the middle value in a dataset, which can be helpful in understanding price distributions.
/// - Serving as a basis for other indicators or trading strategies that require a stable reference point.
///
/// Unlike the mean, the median is not influenced by extreme outliers, making it valuable
/// in markets with occasional large price swings or in the presence of data anomalies.
/// </remarks>
public class Median : AbstractBase
{
    /// <summary>
    /// The number of data points to consider for the median calculation.
    /// </summary>
    private readonly int Period;
    private readonly CircularBuffer _buffer;

    /// <summary>
    /// Initializes a new instance of the Median class.
    /// </summary>
    /// <param name="period">The number of data points to consider. Must be at least 1.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the period is less than 1.
    /// </exception>
    public Median(int period)
    {
        if (period < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(period),
                "Period must be greater than or equal to 1.");
        }
        Period = period;
        WarmupPeriod = period;
        _buffer = new CircularBuffer(period);
        Name = $"Median(period={period})";
        Init();
    }

    /// <summary>
    /// Initializes a new instance of the Median class with a data source.
    /// </summary>
    /// <param name="source">The source object that publishes data.</param>
    /// <param name="period">The number of data points to consider.</param>
    public Median(object source, int period) : this(period)
    {
        var pubEvent = source.GetType().GetEvent("Pub");
        pubEvent?.AddEventHandler(source, new ValueSignal(Sub));
    }

    /// <summary>
    /// Resets the Median indicator to its initial state.
    /// </summary>
    public override void Init()
    {
        base.Init();
        _buffer.Clear();
    }

    /// <summary>
    /// Manages the state of the indicator.
    /// </summary>
    /// <param name="isNew">Indicates if the current data point is new.</param>
    protected override void ManageState(bool isNew)
    {
        if (isNew)
        {
            _lastValidValue = Input.Value;
            _index++;
        }
    }

    /// <summary>
    /// Performs the median calculation.
    /// </summary>
    /// <returns>
    /// The current median value of the dataset.
    /// </returns>
    /// <remarks>
    /// Uses a sorting approach to find the median. If there's not enough data,
    /// it uses the average as a temporary measure.
    /// </remarks>
    protected override double Calculation()
    {
        ManageState(Input.IsNew);
        _buffer.Add(Input.Value, Input.IsNew);

        double median;
        if (_index >= Period)
        {
            var sortedValues = _buffer.GetSpan().ToArray();
            Array.Sort(sortedValues);
            int middleIndex = sortedValues.Length / 2;

            median = (sortedValues.Length % 2 == 0) ? (sortedValues[middleIndex - 1] + sortedValues[middleIndex]) / 2.0 : sortedValues[middleIndex];
        }
        else
        {
            // Not enough data, use average as temporary measure
            median = _buffer.Average();
        }

        IsHot = _index >= WarmupPeriod;
        return median;
    }
}
