using Microsoft.Extensions.Logging;
using ProVantage.Domain.Interfaces;

namespace ProVantage.Infrastructure.Services;

/// <summary>
/// File storage placeholder. In production, swap for Azure Blob / S3.
/// Currently writes to local disk for portfolio demo.
/// </summary>
public class FileStorageService : IFileStorageService
{
    private readonly ILogger<FileStorageService> _logger;
    private readonly string _basePath;

    public FileStorageService(ILogger<FileStorageService> logger)
    {
        _logger = logger;
        _basePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> UploadAsync(string fileName, Stream content, CancellationToken ct = default)
    {
        var uniqueName = $"{Guid.NewGuid():N}_{fileName}";
        var fullPath = Path.Combine(_basePath, uniqueName);
        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream, ct);
        _logger.LogInformation("File uploaded: {FileName}", uniqueName);
        return uniqueName;
    }

    public Task<Stream?> DownloadAsync(string path, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_basePath, path);
        if (!File.Exists(fullPath)) return Task.FromResult<Stream?>(null);
        return Task.FromResult<Stream?>(File.OpenRead(fullPath));
    }

    public Task DeleteAsync(string path, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_basePath, path);
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Email service placeholder. In production, use SendGrid / SMTP.
/// Currently just logs the email.
/// </summary>
public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        _logger.LogInformation("EMAIL SENT → To: {To}, Subject: {Subject}", to, subject);
        return Task.CompletedTask;
    }
}
