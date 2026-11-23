using SmartCare.Application.Common;
using SmartCare.Application.Contracts;

namespace SmartCare.Infrastructure.Services;

/// <summary>
/// Stores uploads on disk OUTSIDE the public web folder under random names. Only PDF, JPG and PNG are accepted,
/// the first bytes must match the extension, and files are only served through a page that checks the user's role.
/// </summary>
public class LocalFileStorage : IFileStorage
{
    private static readonly Dictionary<string, byte[]> Signatures = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = new byte[] { 0x25, 0x50, 0x44, 0x46 },
        [".jpg"] = new byte[] { 0xFF, 0xD8, 0xFF },
        [".jpeg"] = new byte[] { 0xFF, 0xD8, 0xFF },
        [".png"] = new byte[] { 0x89, 0x50, 0x4E, 0x47 }
    };

    private readonly string _root;
    private readonly long _maxBytes;

    public LocalFileStorage(FileStorageOptions options)
    {
        _root = Path.GetFullPath(options.RootPath);
        _maxBytes = options.MaxBytes;
    }

    public async Task<string> SaveAsync(Stream content, string originalFileName, long length, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(originalFileName);
        if (!Signatures.TryGetValue(ext, out var signature))
            throw new BusinessRuleException("Only PDF, JPG and PNG files are allowed.");
        if (length <= 0 || length > _maxBytes)
            throw new BusinessRuleException($"The file must be between 1 byte and {_maxBytes / 1024 / 1024} MB.");
        if (!content.CanSeek)
            throw new BusinessRuleException("The uploaded file could not be read.");

        var header = new byte[4];
        var read = await content.ReadAtLeastAsync(header, 4, throwOnEndOfStream: false, ct);
        if (read < signature.Length || !header.Take(signature.Length).SequenceEqual(signature))
            throw new BusinessRuleException("The file content does not match its extension.");
        content.Position = 0;

        var relative = Path.Combine(DateTime.UtcNow.ToString("yyyy"), DateTime.UtcNow.ToString("MM"),
                                    Guid.NewGuid().ToString("N") + ext.ToLowerInvariant());
        var full = Path.Combine(_root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await using var target = File.Create(full);
        await content.CopyToAsync(target, ct);
        return relative.Replace('\\', '/');
    }

    public Task<Stream> OpenReadAsync(string storagePath, CancellationToken ct = default)
    {
        var full = Path.GetFullPath(Path.Combine(_root, storagePath));
        if (!full.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new BusinessRuleException("Invalid file path.");
        if (!File.Exists(full)) throw new BusinessRuleException("The file was not found on the server.");
        return Task.FromResult<Stream>(File.OpenRead(full));
    }
}
