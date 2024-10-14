namespace QuanTAlib;

public class Mae : AbstractBase
{
    private readonly CircularBuffer _actualBuffer;
    private readonly CircularBuffer _predictedBuffer;

    public Mae(int period)
    {
        if (period < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(period), "Period must be greater than or equal to 1.");
        }
        WarmupPeriod = period;
        _actualBuffer = new CircularBuffer(period);
        _predictedBuffer = new CircularBuffer(period);
        Name = $"Mae(period={period})";
        Init();
    }

    public Mae(object source, int period) : this(period)
    {
        var pubEvent = source.GetType().GetEvent("Pub");
        pubEvent?.AddEventHandler(source, new ValueSignal(Sub));
    }

    public override void Init()
    {
        base.Init();
        _actualBuffer.Clear();
        _predictedBuffer.Clear();
    }

    protected override void ManageState(bool isNew)
    {
        if (isNew)
        {
            _lastValidValue = Input.Value;
            _index++;
        }
    }

    protected override double Calculation()
    {
        ManageState(Input.IsNew);

        double actual = Input.Value;
        _actualBuffer.Add(actual, Input.IsNew);

        double predicted = double.IsNaN(Input2.Value) ? _actualBuffer.Average() : Input2.Value;
        _predictedBuffer.Add(predicted, Input.IsNew);

        double mae = 0;
        if (_actualBuffer.Count > 0)
        {
            var actualValues = _actualBuffer.GetSpan().ToArray();
            var predictedValues = _predictedBuffer.GetSpan().ToArray();

            double sumAbsoluteError = 0;
            for (int i = 0; i < _actualBuffer.Count; i++)
            {
                sumAbsoluteError += Math.Abs(actualValues[i] - predictedValues[i]);
            }

            mae = sumAbsoluteError / _actualBuffer.Count;
        }

        IsHot = _index >= WarmupPeriod;
        return mae;
    }

}
