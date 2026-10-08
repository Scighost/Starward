using Starward.Core.HoYoPlay;

namespace Starward.GameManagement.Model;


internal abstract class SophonFileItem
{
    public bool IsFinished { get; set; }

    public bool IsHardlinked { get; set; }

    public long NetworkSize { get; set; }

    public long StorageSize { get; set; }

    public GameSophonManifestUrl ManifestUrl { get; set; }
}


internal class SophonChunkItem : SophonFileItem
{
    public SophonChunkFile ChunkFile { get; set; }

    public SophonChunk? Chunk { get; set; }

    public SophonChunkItem(GameSophonManifestUrl manifestUrl, SophonChunkFile chunkFile, SophonChunk? chunk = null)
    {
        ManifestUrl = manifestUrl;
        ChunkFile = chunkFile;
        Chunk = chunk;
    }
}


internal class SophonPatchItem : SophonFileItem
{
    public SophonPatchFile PatchFile { get; set; }

    public SophonDiffFile DiffFile { get; set; }

    public SophonPatchItem(GameSophonManifestUrl manifestUrl, SophonPatchFile patchFile, SophonDiffFile diffFile)
    {
        ManifestUrl = manifestUrl;
        PatchFile = patchFile;
        DiffFile = diffFile;
    }
}
