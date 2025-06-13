namespace Snap.HPatch;


internal readonly partial struct PCSTR
{
#pragma warning disable CS0649
    public readonly unsafe byte* Value;
#pragma warning restore CS0649

    public static unsafe implicit operator PCSTR(byte* value)
    {
        return *(PCSTR*)&value;
    }

    public static unsafe implicit operator byte*(PCSTR value)
    {
        return *(byte**)&value;
    }
}