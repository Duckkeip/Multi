using System.Net.Sockets;
using ChatProtocol;

namespace ChatServer;

/// <summary>
/// Boc mot ket noi TCP (1 client) + trang thai cua no (username, phong, da xac thuc chua).
/// Gui du lieu qua FrameCodec (length-prefix JSON) dinh nghia trong ChatProtocol.
/// </summary>
public class ClientSession
{
    public TcpClient TcpClient { get; }
    public Stream Stream { get; }
    public string Username { get; set; } = "";
    public string? CurrentRoom { get; set; }
    public bool IsAuthenticated { get; set; }
    public PendingRegistration? PendingRegistration { get; set; }
    public string? PendingPasswordChangeToken { get; set; }
    public string? PendingPasswordResetToken { get; set; }
    public PendingServerCreation? PendingServerCreation { get; set; }
    public Dictionary<string, PendingUpload> Uploads { get; } = new();
    
    // Session tracking
    public string SessionId { get; } = Guid.NewGuid().ToString();
    public string DeviceInfo { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public DateTime LoginTime { get; set; } = DateTime.UtcNow;
    public DateTime LastActive { get; set; } = DateTime.UtcNow;
    public bool TwoFactorVerified { get; set; } = false;
    public string? VoiceChannelId { get; set; }
    public DateTime VoiceJoinedAt { get; set; }
    public bool VoiceMicrophoneEnabled { get; set; }
    public bool VoiceCameraEnabled { get; set; }

    // SemaphoreSlim thay vi "lock" thuong, vi FrameCodec.WriteAsync la ham async
    // (khong the giu "lock" C# qua mot "await").
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public ClientSession(TcpClient tcpClient, Stream stream)
    {
        TcpClient = tcpClient;
        Stream = stream;
        
        // Get client IP address
        if (tcpClient.Client.RemoteEndPoint is System.Net.IPEndPoint endPoint)
        {
            IpAddress = endPoint.Address.ToString();
        }
    }

    /// <summary>Gui 1 message (type + payload) toi client nay.</summary>
    public async Task SendAsync(string type, object? data = null)
    {
        await _writeLock.WaitAsync();
        try
        {
            if (TcpClient.Connected)
                await FrameCodec.WriteAsync(Stream, type, data);
        }
        catch
        {
            // Client da ngat ket noi giua chung; vong lap doc chinh se don dep sau.
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public void Close()
    {
        try { Stream.Close(); } catch { /* ignore */ }
        try { TcpClient.Close(); } catch { /* ignore */ }
    }
}

public sealed class PendingRegistration
{
    public string Username { get; }
    public string Password { get; }
    public string Email { get; }
    public string RegistrationToken { get; }

    public PendingRegistration(string username, string password, string email, string registrationToken)
    {
        Username = username;
        Password = password;
        Email = email;
        RegistrationToken = registrationToken;
    }
}

public sealed class PendingServerCreation
{
    public string Name { get; }
    public string? Description { get; }
    public string VerificationToken { get; }

    public PendingServerCreation(string name, string? description, string verificationToken)
    {
        Name = name;
        Description = description;
        VerificationToken = verificationToken;
    }
}

public sealed class PendingUpload : IDisposable
{
    public string FileName { get; }
    public long ExpectedSize { get; }
    public string? ThumbnailBase64 { get; }
    public int NextChunkIndex { get; set; }
    public MemoryStream Content { get; } = new();

    public PendingUpload(string fileName, long expectedSize, string? thumbnailBase64 = null)
    {
        FileName = fileName;
        ExpectedSize = expectedSize;
        ThumbnailBase64 = thumbnailBase64;
    }

    public void Dispose() => Content.Dispose();
}
