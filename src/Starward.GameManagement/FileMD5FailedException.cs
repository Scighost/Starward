namespace Starward.GameManagement;

internal class FileMD5FailedException : Exception
{
    public string FileName { get; init; }

    public string CorrectMD5 { get; init; }

    public FileMD5FailedException(string fileName, string correctMD5)
        : base($"File MD5 check failed. File: {fileName}, Correct MD5: {correctMD5}")
    {
        FileName = fileName;
        CorrectMD5 = correctMD5;
    }
}