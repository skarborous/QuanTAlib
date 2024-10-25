namespace QuanTAlib;

/// <summary>
/// Represents a minimum value calculator with optional decay over a specified period.
/// This class calculates the minimum value within a given period, with the ability to
/// apply a decay factor to give more weight to recent values.
/// </summary>
/// <remarks>
/// The Min class uses a circular buffer to store values and calculates the minimum
/// efficiently. It also implements a decay mechanism to adjust the minimum value over
/// time, allowing for a more responsive indicator in changing market conditions.
///
/// The decay factor allows the indicator to "forget" old minimum values gradually,
/// which can be useful in adapting to new price trends or market regimes.
/// </remarks>
public class Min : AbstractBase
{
    /// <summary>
    /// The number of data points to consider for the minimum calculation.
    /// </summary>
    private readonly int Period;

    /// <summary>
    /// Circular buffer to store the most recent data points.
    /// </summary>
    private readonly CircularBuffer _buffer;

    /// <summary>
    /// The half-life decay factor used to gradually forget old minimums.
    /// </summary>
    private readonly double _halfLife;

    /// <summary>
    /// The current minimum value.
    /// </summary>
    private double _currentMin;

    /// <summary>
    /// The previous minimum value.
    /// </summary>
    private double _p_currentMin;

    /// <summary>
    /// The number of periods since a new minimum was set.
    /// </summary>
    private int _timeSinceNewMin;

    /// <summary>
    /// The previous value of _timeSinceNewMin.
    /// </summary>
    private int _p_timeSinceNewMin;

    /// <summary>
    /// Initializes a new instance of the Min class with the specified period and decay.
    /// </summary>
    /// <param name="period">The period over which to calculate the minimum value.</param>
    /// <param name="decay">The decay factor to apply to older values. Higher values cause faster forgetting of old minimums. Default is 0 (no decay).</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when period is less than 1 or decay is negative.
    /// </exception>
    public Min(int period, double decay = 0)
    {
        if (period < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(period), "Period must be greater than or equal to 1.");
        }
        if (decay < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(decay), "Half-life must be non-negative.");
        }
        Period = period;
        WarmupPeriod = 0;
        _buffer = new CircularBuffer(period);
        _halfLife = decay * 0.1;
        Name = $"Min(period={period}, halfLife={decay:F2})";
        Init();
    }

    /// <summary>
    /// Initializes a new instance of the Min class with the specified source, period, and decay.
    /// </summary>
    /// <param name="source">The source object to subscribe to for value updates.</param>
    /// <param name="period">The period over which to calculate the minimum value.</param>
    /// <param name="decay">The decay factor to apply to older values. Higher values cause faster forgetting of old minimums. Default is 0 (no decay).</param>
    public Min(object source, int period, double decay = 0) : this(period, decay)
    {
        var pubEvent = source.GetType().GetEvent("Pub");
        pubEvent?.AddEventHandler(source, new ValueSignal(Sub));
    }

    /// <summary>
    /// Initializes the Min instance by setting initial values.
    /// </summary>
    public override void Init()
    {
        base.Init();
        _currentMin = double.MaxValue;
        _timeSinceNewMin = 0;
    }

    /// <summary>
    /// Manages the state of the Min instance based on whether a new value is being processed.
    /// </summary>
    /// <param name="isNew">Indicates whether the current input is a new value.</param>
    protected override void ManageState(bool isNew)
    {
        if (isNew)
        {
            _p_currentMin = _currentMin;
            _lastValidValue = Input.Value;
            _index++;
            _timeSinceNewMin++;
            _p_timeSinceNewMin = _timeSinceNewMin;
        }
        else
        {
            _currentMin = _p_currentMin;
            _timeSinceNewMin = _p_timeSinceNewMin;
        }
    }

    /// <summary>
    /// Performs the minimum value calculation with decay.
    /// </summary>
    /// <returns>The calculated minimum value for the current period.</returns>
    /// <remarks>
    /// This method updates the current minimum value based on the input, applies the decay
    /// factor, and ensures the result is not lower than the actual minimum in the buffer.
    /// The decay rate is calculated using an exponential function based on the time since
    /// the last new minimum and the specified half-life.
    /// </remarks>
    protected override double Calculation()
    {
        ManageState(Input.IsNew);
        _buffer.Add(Input.Value, Input.IsNew);

        if (Input.Value <= _currentMin)
        {
            _currentMin = Input.Value;
            _timeSinceNewMin = 0;
        }

        double decayRate = 1 - Math.Exp(-_halfLife * _timeSinceNewMin / Period);
        _currentMin += decayRate * (_buffer.Average() - _currentMin);
        _currentMin = Math.Max(_currentMin, _buffer.Min());

        IsHot = true;
        return _currentMin;
    }
}
