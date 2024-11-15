namespace Starward.Core.GameRecord.ZZZ.InterKnotReport;

public abstract class InterKnotReportDataType
{

    /// <summary>
    /// 菲林
    /// </summary>
    public const string PolychromesData = nameof(PolychromesData);

    /// <summary>
    /// 加密母带 & 原装母带
    /// </summary>
    public const string MatserTapeData = nameof(MatserTapeData);

    /// <summary>
    /// 邦布券
    /// </summary>
    public const string BooponsData = nameof(BooponsData);



    public static string ToLocalization(string type)
    {
        return type switch
        {
            _ => type,
        };
    }



}