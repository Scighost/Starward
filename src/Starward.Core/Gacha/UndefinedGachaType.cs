namespace Starward.Core.Gacha;

public readonly record struct UndefinedGachaType(int Value) : IGachaType
{

    public string ToLocalization() => Value.ToString();

    public override string ToString() => Value.ToString();


    public static implicit operator UndefinedGachaType(int value) => new(value);
    public static implicit operator int(UndefinedGachaType value) => value.Value;

}
