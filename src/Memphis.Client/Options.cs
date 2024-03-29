using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

#nullable disable 

namespace Memphis.Client;

public sealed class TlsOptions
{
    public TlsOptions(string fileName)
        => (FileName) = (fileName);

    public TlsOptions(string fileName, string password) : this(fileName)
        => (Password) = (password);

    public TlsOptions(X509Certificate2 certificate)
        => (Certificate) = (certificate);

    public X509Certificate2 Certificate { get; set; }
    public string FileName { get; set; }
    public string Password { get; set; }
    public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; }
}

public sealed class ClientOptions
{
    public string Host { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string ConnectionToken { get; set; }
    public int Port { get; set; }
    public bool Reconnect { get; set; }
    /// <summary>
    /// Gets or sets the maximum number of times a connection will
    /// attempt to reconnect. To reconnect indefinitely set this value to -1.
    /// </summary>
    public int MaxReconnect { get; set; } = Options.ReconnectForever;
    public int MaxReconnectIntervalMs { get; set; }
    public int TimeoutMs { get; set; }
    public TlsOptions Tls { get; set; }

    /// <summary>
    /// The AccountId field should be set only on the cloud version of Memphis, otherwise it will be ignored.
    /// </summary>
    public int AccountId { get; set; }

    public EventHandler<MemphisConnectionEventArgs> ClosedEventHandler;
    
}