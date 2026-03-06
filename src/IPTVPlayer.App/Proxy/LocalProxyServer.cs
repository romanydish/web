using System.Net;
using System.Net.Http.Headers;

namespace IPTVPlayer.App.Proxy;

public class LocalProxyServer : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly HttpClient _httpClient = new();
    private CancellationTokenSource? _cts;

    public int Port { get; }

    public LocalProxyServer(int port = 8989)
    {
        Port = port;
        _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
    }

    public void Start()
    {
        if (_listener.IsListening)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _listener.Start();
        _ = Task.Run(() => ProcessAsync(_cts.Token));
    }

    public void Stop()
    {
        _cts?.Cancel();
        if (_listener.IsListening)
        {
            _listener.Stop();
        }
    }

    public string BuildProxyUrl(string upstreamUrl, string? token = null)
    {
        var encoded = Uri.EscapeDataString(upstreamUrl);
        if (string.IsNullOrWhiteSpace(token))
        {
            return $"http://127.0.0.1:{Port}/stream?url={encoded}";
        }

        return $"http://127.0.0.1:{Port}/stream?url={encoded}&token={Uri.EscapeDataString(token)}";
    }

    private async Task ProcessAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            HttpListenerContext? context = null;
            try
            {
                context = await _listener.GetContextAsync();
                _ = Task.Run(() => HandleContextAsync(context, cancellationToken), cancellationToken);
            }
            catch when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                context?.Response.Abort();
            }
        }
    }

    private async Task HandleContextAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        var url = context.Request.QueryString["url"];
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            context.Response.StatusCode = 400;
            context.Response.Close();
            return;
        }

        using var req = new HttpRequestMessage(HttpMethod.Get, uri);
        req.Headers.UserAgent.Add(new ProductInfoHeaderValue("IPTVPlayer", "1.0"));
        using var upstream = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        context.Response.StatusCode = (int)upstream.StatusCode;

        if (upstream.Content.Headers.ContentType != null)
        {
            context.Response.ContentType = upstream.Content.Headers.ContentType.ToString();
        }

        await using var inStream = await upstream.Content.ReadAsStreamAsync(cancellationToken);
        await inStream.CopyToAsync(context.Response.OutputStream, cancellationToken);
        context.Response.Close();
    }

    public void Dispose()
    {
        Stop();
        _listener.Close();
        _httpClient.Dispose();
    }
}
