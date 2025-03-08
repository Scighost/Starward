using SharpSevenZip.EventArguments;

namespace SharpSevenZip;

/// <summary>
/// Callback delegate for <see cref="SharpSevenZipExtractor.ExtractFiles(ExtractFileCallback)"/>.
/// </summary>
public delegate void ExtractFileCallback(ExtractFileCallbackArgs extractFileCallbackArgs);
