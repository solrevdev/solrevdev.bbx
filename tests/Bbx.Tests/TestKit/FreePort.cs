using System.Net;
using System.Net.Sockets;

namespace Bbx.Tests.TestKit;

internal static class FreePort
{
    public static int Pick()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }
}
