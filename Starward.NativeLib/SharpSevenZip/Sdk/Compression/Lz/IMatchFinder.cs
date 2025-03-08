namespace SharpSevenZip.Sdk.Compression.Lz;

internal interface IInWindowStream
{
    void SetStream(Stream inStream);
    void Init();
    void ReleaseStream();
    byte GetIndexByte(int index);
    uint GetMatchLen(int index, uint distance, uint limit);
    uint GetNumAvailableBytes();
}

internal interface IMatchFinder : IInWindowStream
{
    void Create(uint historySize, uint keepAddBufferBefore,
                uint matchMaxLen, uint keepAddBufferAfter);

    uint GetMatches(uint[] distances);
    void Skip(uint num);
}
