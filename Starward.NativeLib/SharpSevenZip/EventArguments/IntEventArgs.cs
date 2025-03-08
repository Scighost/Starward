namespace SharpSevenZip.EventArguments;

/// <summary>
/// Stores an int number
/// </summary>
public readonly struct IntEventArgs
{
    /// <summary>
    /// Initializes a new instance of the IntEventArgs class
    /// </summary>
    /// <param name="value">Useful data carried by the IntEventArgs class</param>
    public IntEventArgs(int value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the value of the IntEventArgs class
    /// </summary>
    public int Value { get; init; }
}
