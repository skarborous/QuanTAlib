namespace QuanTAlib;

/// <summary>
/// Calculates the maximum value over a specified period, with an optional decay factor.
/// Useful for tracking the highest point in a time series with the ability to gradually forget old peaks.
/// </summary>
/// <remarks>
/// The Max indicator is particularly useful in financial analysis for:
/// - Identifying resistance levels in price charts.
/// - Tracking the highest price over a given period.
/// - Implementing trailing stop-loss strategies.
///
/// The decay factor allows the indicator to adapt to changing market conditions by
/// gradually reducing the influence of older maximum values.
/// </remarks>
public class Max : AbstractBase
{
    /// <summary>
    /// The number of data points to consider for the maximum calculation.
    /// </summary>
    private readonly int Period;

    /// <summary>
    /// Circular buffer to store the most recent data points.
    /// </summary>
    private readonly CircularBuffer _buffer;

    /// <summary>
    /// The half-life decay factor used to gradually forget old peaks.
    /// </summary>
    private readonly double _halfLife;

    /// <summary>
    /// The current maximum value.
    /// </summary>
    private double _currentMax;

    /// <summary>
    /// The previous maximum value.
    /// </summary>
    private double _p_currentMax;

    /// <summary>
    /// The number of periods since a new maximum was set.
    /// </summary>
    private int _timeSinceNewMax;

    /// <summary>
    /// The previous value of _timeSinceNewMax.
    /// </summary>
    private int _p_timeSinceNewMax;

    /// <summary>
    /// Initializes a new instance of the Max class.
    /// </summary>
    /// <param name="period">The number of data points to consider. Must be at least 1.</param>
    /// <param name="decay">Half-life decay factor. Set to 0 for no decay, higher for faster forgetting of old peaks. Default is 0.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the period is less than 1 or decay is negative.
    /// </exception>
    public Max(int period, double decay = 0)
    {
        if (period < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(period),
                "Period must be greater than or equal to 1.");
        }
        if (decay < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(decay),
                "Half-life must be non-negative.");
        }
        Period = period;
        WarmupPeriod = 0;
        _buffer = new CircularBuffer(period);
        _halfLife = decay * 0.1;
        Name = $"Max(period={period}, halfLife={decay:F2})";
        Init();
    }

    /// <summary>
    /// Initializes a new instance of the Max class with a data source.
    /// </summary>
    /// <param name="source">The source object that publishes data.</param>
    /// <param name="period">The number of data points to consider.</param>
    /// <param name="decay">Half-life decay factor. Default is 0.</param>
    public Max(object source, int period, double decay = 0) : this(period, decay)
    {
        var pubEvent = source.GetType().GetEvent("Pub");
        pubEvent?.AddEventHandler(source, new ValueSignal(Sub));
    }

    /// <summary>
    /// Resets the Max indicator to its initial state.
    /// </summary>
    public override void Init()
    {
        base.Init();
        _currentMax = double.MinValue;
        _timeSinceNewMax = 0;
    }

    /// <summary>
    /// Manages the state of the indicator.
    /// </summary>
    /// <param name="isNew">Indicates if the current data point is new.</param>
    protected override void ManageState(bool isNew)
    {
        if (isNew)
        {
            _p_currentMax = _currentMax;
            _lastValidValue = Input.Value;
            _index++;
            _timeSinceNewMax++;
            _p_timeSinceNewMax = _timeSinceNewMax;
        }
        else
        {
            _currentMax = _p_currentMax;
            _timeSinceNewMax = _p_timeSinceNewMax;
        }
    }

    /// <summary>
    /// Performs the max calculation.
    /// </summary>
    /// <returns>
    /// The current maximum value, potentially adjusted by the decay factor.
    /// </returns>
    /// <remarks>
    /// Uses a decay factor to gradually forget old peaks. The max value is always
    /// capped by the highest value in the current period.
    /// </remarks>
    protected override double Calculation()
    {
        ManageState(Input.IsNew);
        _buffer.Add(Input.Value, Input.IsNew);

        if (Input.Value >= _currentMax)
        {
            _currentMax = Input.Value;
            _timeSinceNewMax = 0;
        }

        double decayRate = 1 - Math.Exp(-_halfLife * _timeSinceNewMax / Period);
        _currentMax -= decayRate * (_currentMax - _buffer.Average());
        _currentMax = Math.Min(_currentMax, _buffer.Max());

        IsHot = true;
        return _currentMax;
    }
}
