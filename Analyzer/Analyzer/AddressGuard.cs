using System.Net;
using System.Net.Sockets;

namespace Analyzer
{
    public class TargetNotAllowedException : Exception
    {
        public TargetNotAllowedException() : base("The target address is not allowed.")
        {
        }
    }

    // Refuses connections to local and private networks (SSRF protection). The check runs on the addresses
    // actually used for the connection, so it also covers DNS names and redirects.
    public static class AddressGuard
    {
        public static bool IsRestricted(IPAddress address)
        {
            if (address.IsIPv4MappedToIPv6)
            {
                address = address.MapToIPv4();
            }

            if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
            {
                return true;
            }

            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                byte[] b = address.GetAddressBytes();
                return b[0] == 0                                    // 0.0.0.0/8
                    || b[0] == 10                                   // 10.0.0.0/8
                    || (b[0] == 100 && b[1] >= 64 && b[1] <= 127)   // 100.64.0.0/10 (carrier-grade NAT)
                    || (b[0] == 169 && b[1] == 254)                 // 169.254.0.0/16 (link-local, cloud metadata)
                    || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)    // 172.16.0.0/12
                    || (b[0] == 192 && b[1] == 168)                 // 192.168.0.0/16
                    || b[0] >= 224;                                 // multicast and reserved
            }

            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                byte[] b = address.GetAddressBytes();
                return address.IsIPv6LinkLocal
                    || address.IsIPv6SiteLocal
                    || address.IsIPv6Multicast
                    || (b[0] & 0xFE) == 0xFC;                       // fc00::/7 (unique local)
            }

            return true;
        }

        public static async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context,
            bool allowPrivateAddresses, CancellationToken cancellationToken)
        {
            string host = context.DnsEndPoint.Host;

            IPAddress[] addresses = IPAddress.TryParse(host, out IPAddress? literal)
                ? new[] { literal }
                : await Dns.GetHostAddressesAsync(host, cancellationToken);

            if (addresses.Length == 0)
            {
                throw new SocketException((int)SocketError.HostNotFound);
            }

            // A name that resolves to any restricted address is refused as a whole.
            if (!allowPrivateAddresses && addresses.Any(IsRestricted))
            {
                throw new TargetNotAllowedException();
            }

            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await socket.ConnectAsync(addresses, context.DnsEndPoint.Port, cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }
    }
}
