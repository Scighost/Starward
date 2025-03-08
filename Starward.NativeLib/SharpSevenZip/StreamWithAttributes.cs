namespace SharpSevenZip;
public record StreamWithAttributes(Stream Stream, DateTime? CreationTime = null, DateTime? LastWriteTime = null, DateTime? LastAccessTime = null);