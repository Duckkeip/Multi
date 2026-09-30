using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

class Program
{
    static void Main()
    {
        var cert = X509Certificate2.CreateFromPemFile("server.crt", "server.key");
        var pfxBytes = cert.Export(X509ContentType.Pkcs12, "177013");
        File.WriteAllBytes("chatnet-server.pfx", pfxBytes);
        Console.WriteLine("Tao file chatnet-server.pfx thanh cong!");
    }
}
