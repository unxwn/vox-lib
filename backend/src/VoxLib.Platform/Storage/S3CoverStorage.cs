using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VoxLib.Model.Storage;

namespace VoxLib.Platform.Storage;

/// <summary>
/// Cover storage over the S3 API. One implementation for MinIO and for
/// Cloudflare R2, with nothing but configuration between them — which is what
/// makes changing provider a configuration change rather than a data migration
/// (FR-043).
/// <para>
/// The S3 SDK rather than a MinIO client, deliberately: it is what Cloudflare's
/// own R2 documentation targets, so R2 is the production case and MinIO the
/// stand-in, not the other way round.
/// </para>
/// </summary>
public sealed class S3CoverStorage : ICoverStorage, IDisposable
{
    private readonly CoverStorageOptions _options;
    private readonly ILogger<S3CoverStorage> _logger;
    private readonly IAmazonS3? _client;

    public S3CoverStorage(
        IOptions<CoverStorageOptions> options,
        ILogger<S3CoverStorage> logger)
    {
        _options = options.Value;
        _logger = logger;

        // No endpoint configured is a valid state rather than a failure: the
        // catalogue still serves, and every book shows the placeholder. A
        // constructor that threw would make storage a startup dependency of the
        // whole API, which is exactly what FR-051 says it must not be.
        if (string.IsNullOrWhiteSpace(_options.ServiceUrl))
        {
            _logger.LogWarning(
                "Cover storage has no ServiceUrl configured. Covers will not be stored "
                    + "or served, and every book will show the placeholder.");
            return;
        }

        _client = new AmazonS3Client(
            new BasicAWSCredentials(_options.AccessKey, _options.SecretKey),
            new AmazonS3Config
            {
                ServiceURL = _options.ServiceUrl,
                ForcePathStyle = _options.ForcePathStyle,

                // Required by the SDK even where the provider ignores it. R2
                // uses "auto"; MinIO does not care.
                AuthenticationRegion = "auto",
            });
    }

    /// <summary>
    /// Composition only. It touches no network and cannot fail, which is the
    /// whole reason a catalogue response never fails because of storage.
    /// </summary>
    public Uri PublicUrl(string coverKey, int width) =>
        new($"{_options.PublicBaseUrl.TrimEnd('/')}/{CoverWidths.ObjectKey(coverKey, width)}");

    public async Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken)
    {
        if (_client is null)
        {
            return false;
        }

        try
        {
            await _client.GetObjectMetadataAsync(
                _options.Bucket,
                objectKey,
                cancellationToken);

            return true;
        }
        catch (AmazonS3Exception found) when (found.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task PutAsync(
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        if (_client is null)
        {
            throw new InvalidOperationException(
                "Cover storage has no ServiceUrl configured, so nothing can be stored.");
        }

        await _client.PutObjectAsync(
            new PutObjectRequest
            {
                BucketName = _options.Bucket,
                Key = objectKey,
                InputStream = content,
                ContentType = contentType,

                // Not DisablePayloadSigning: R2 accepts signed payloads and so
                // does MinIO, and turning it off to suit one of them is the kind
                // of development-only branch this adapter exists without.
                DisableDefaultChecksumValidation = true,
            },
            cancellationToken);
    }

    public void Dispose() => _client?.Dispose();
}
