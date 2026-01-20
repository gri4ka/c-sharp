using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using tianzi.Data;
using tianzi.Models;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;

namespace tianzi.Controllers;

public class FilesController : Controller
{
    private readonly AppDbContext _db;
    private readonly ILogger<FilesController> _logger;
    private readonly IWebHostEnvironment _env;

    private const long MaxUploadBytes = 10 * 1024 * 1024; // 10 MB demo limit

    public FilesController(AppDbContext db, ILogger<FilesController> logger, IWebHostEnvironment env)
    {
        _db = db;
        _logger = logger;
        _env = env;
    }

    [HttpGet]
    public IActionResult Index() => View();

    [HttpGet]
    public IActionResult Lookup() => View();

    [HttpPost]
    public IActionResult Lookup(string code)
    {
        code = (code ?? "").Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(code))
            return RedirectToAction(nameof(Lookup));

        return RedirectToAction(nameof(Details), new { code });
    }

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        try
        {
            
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Pick a file first.";
                return RedirectToAction(nameof(Index));
            }

            if (file.Length > MaxUploadBytes)
            {
                TempData["Error"] = $"File too large (max {MaxUploadBytes / (1024 * 1024)} MB).";
                return RedirectToAction(nameof(Index));
            }

            // Never trust file paths from the browser
            var safeName = Path.GetFileName(file.FileName);
            var contentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType;

            await using var ms = new MemoryStream();
            await file.CopyToAsync(ms);

            var code = await GenerateUniqueCodeAsync(8);//len in chars
            var deleteToken = await GenerateUniqueDeleteTokenAsync(8);//len in bytes
            var entity = new SharedFile
            {
                Code = code,
                DeleteToken = deleteToken,
                OriginalFileName = safeName,
                ContentType = contentType,
                Size = file.Length,
                Data = ms.ToArray(),
                UploadedAtUtc = DateTime.UtcNow,
                DownloadCount = 0
            };

            _db.SharedFiles.Add(entity);
            await _db.SaveChangesAsync();

            TempData["Code"] = code;
            TempData["DeleteToken"] = deleteToken;

            return RedirectToAction(nameof(Success));
        }
        catch (Exception ex)
        {
            TempData["Error"] = "An internal error occurred while uploading. Check server logs for details." + ex;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public IActionResult Success()
    {
        if (TempData["Code"] == null || TempData["DeleteToken"] == null)
            return RedirectToAction(nameof(Index));

        return View();
    }

    [HttpGet("/d/{code}")]
    public async Task<IActionResult> Details(string code)
    {
        code = (code ?? "").Trim().ToUpperInvariant();

        var f = await _db.SharedFiles
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Code == code);

        if (f == null) return View("Missing");

        return View(f);
    }

    [HttpGet("/f/{code}")]
    public async Task<IActionResult> Download(string code)
    {
        code = (code ?? "").Trim().ToUpperInvariant();

        var f = await _db.SharedFiles.SingleOrDefaultAsync(x => x.Code == code);
        if (f == null) return View("Missing");

        f.DownloadCount += 1;
        await _db.SaveChangesAsync();

        return File(f.Data, f.ContentType, f.OriginalFileName);
    }
    [HttpPost]
    public async Task<IActionResult> Delete(string code, string delcode)
    {
        code = (code ?? "").Trim().ToUpperInvariant();
        delcode = (delcode ?? "").Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(delcode))
            return View("Missing");

        var f = await _db.SharedFiles.SingleOrDefaultAsync(x => x.Code == code && x.DeleteToken == delcode);
        if (f == null) return View("Missing");

        _db.SharedFiles.Remove(f);
        await _db.SaveChangesAsync();

        return Content("Deleted. (You can close this tab.)");
    }

    [HttpGet]
    public IActionResult Missing() => View();

    private async Task<string> GenerateUniqueCodeAsync(int length)
    {
        // Base32-ish alphabet without confusing chars
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        while (true)
        {
            var chars = new char[length];
            for (int i = 0; i < length; i++)
                chars[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];

            var code = new string(chars);

            var exists = await _db.SharedFiles.AnyAsync(x => x.Code == code);
            if (!exists) return code;
        }
    }

    private async Task<string> GenerateUniqueDeleteTokenAsync(int bytes)
    {
        while (true)
        {
            var buf = RandomNumberGenerator.GetBytes(bytes);
            var token = Convert.ToHexString(buf);

            var exists = await _db.SharedFiles.AnyAsync(x => x.DeleteToken == token);
            if (!exists) return token;
        }
    }
}