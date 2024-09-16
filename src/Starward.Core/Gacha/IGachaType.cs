namespace Starward.Core.Gacha;

public interface IGachaType
{

    public int Value { get; init; }


    public string ToLocalization();

}
