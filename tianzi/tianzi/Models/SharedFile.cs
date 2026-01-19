using System.ComponentModel.DataAnnotations;

namespace tianzi.Models;

public class SharedFile
{
    public int Id { get; set; }

    [Required, MaxLength(16)]
    public string Code { get; set; } = "";

    [Required, MaxLength(64)]
    public string DeleteToken { get; set; } = "";

    [Required, MaxLength(260)]
    public string OriginalFileName { get; set; } = "";

    [Required, MaxLength(200)]
    public string ContentType { get; set; } = "application/octet-stream";

    public long Size { get; set; }

    [Required]
    public byte[] Data { get; set; } = Array.Empty<byte>();

    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;

    public int DownloadCount { get; set; }
}
