namespace Foodeez.Application.Common;

/// <summary>
/// What an uploaded image actually is, as opposed to what the upload claimed it was.
///
/// The media type is handed straight to the vision provider - Anthropic's `media_type`,
/// Gemini's `mimeType`, a data URI for a local server - and all of them validate it against a
/// fixed list and refuse the whole request when it is not on it. So a client that derives the
/// type from a file extension ("photo.jpg" becoming "image/jpg", which is not a media type)
/// turns every photograph into "that could not be read automatically", with the real reason
/// visible only in the API log.
///
/// The bytes are the only thing here that cannot be wrong, so they are what is trusted: every
/// format in question is identified by a fixed signature in its first few bytes. The declared
/// type is a fallback for anything the sniffer does not recognise, and even then it is
/// normalised against the same list rather than passed through.
/// </summary>
public static class ImageMediaType
{
    /// <summary>What a phone camera produces, and the assumption when nothing better is known.</summary>
    public const string Jpeg = "image/jpeg";

    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = "image/jpeg",
        ["image/jpg"] = "image/jpeg",
        ["image/pjpeg"] = "image/jpeg",
        ["image/png"] = "image/png",
        ["image/gif"] = "image/gif",
        ["image/webp"] = "image/webp",
        ["image/heic"] = "image/heic",
        ["image/heif"] = "image/heic",
    };

    /// <summary>
    /// The media type to send to the provider: read from the image itself where the format is
    /// recognised, otherwise the declared type cleaned up, otherwise JPEG.
    /// </summary>
    public static string Resolve(byte[] imageData, string? declared) =>
        Sniff(imageData)
        ?? (declared is not null && Aliases.TryGetValue(Trim(declared), out var normalised) ? normalised : Jpeg);

    /// <summary>The declared type without any "; charset=..." or padding a client may have added.</summary>
    private static string Trim(string declared)
    {
        var semicolon = declared.IndexOf(';');
        return (semicolon >= 0 ? declared[..semicolon] : declared).Trim();
    }

    /// <summary>The format's own signature, or null when these bytes are not a format we name.</summary>
    private static string? Sniff(byte[] data)
    {
        if (StartsWith(data, 0xFF, 0xD8, 0xFF)) return "image/jpeg";
        if (StartsWith(data, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A)) return "image/png";
        if (StartsWith(data, 0x47, 0x49, 0x46, 0x38)) return "image/gif";

        // Both of these are containers: a four-byte tag, a length or size field, then the tag
        // that says which format is inside. WEBP is "RIFF" + size + "WEBP"; HEIC is a size +
        // "ftyp" + a brand, of which several mean HEIC.
        if (data.Length >= 12 && Matches(data, 0, "RIFF") && Matches(data, 8, "WEBP")) return "image/webp";
        if (data.Length >= 12 && Matches(data, 4, "ftyp") && IsHeicBrand(data)) return "image/heic";

        return null;
    }

    private static bool IsHeicBrand(byte[] data)
    {
        string[] brands = ["heic", "heix", "heim", "heis", "hevc", "hevx", "hevm", "hevs", "mif1", "msf1"];
        return brands.Any(brand => Matches(data, 8, brand));
    }

    private static bool StartsWith(byte[] data, params byte[] signature) =>
        data.Length >= signature.Length && signature.Select((b, i) => data[i] == b).All(match => match);

    private static bool Matches(byte[] data, int offset, string tag) =>
        data.Length >= offset + tag.Length
        && !tag.Where((c, i) => data[offset + i] != (byte)c).Any();
}
