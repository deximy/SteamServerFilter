using System.Net;
using System.Net.Sockets;

namespace SteamServerFilter
{
    public class SniffingServerNameService
    {
        private readonly UdpClient sniffing_socket_;
        private readonly byte[] a2s_query_data_;
        private readonly byte[] a2s_query_challange_prefix_;

        public SniffingServerNameService()
        {
            sniffing_socket_ = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
            LogService.Debug($"Local sniffing socket port: {((IPEndPoint?)sniffing_socket_.Client.LocalEndPoint)?.Port}");

            a2s_query_data_ = new byte[] {
                0xFF, 0xFF, 0xFF, 0xFF,
                0x54, 0x53, 0x6F, 0x75,
                0x72, 0x63, 0x65, 0x20,
                0x45, 0x6E, 0x67, 0x69,
                0x6E, 0x65, 0x20, 0x51,
                0x75, 0x65, 0x72, 0x79,
                0x00
            };
            a2s_query_challange_prefix_ = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x41 };

            Task.Run(
                async () => {

                    while (true)
                    {
                        var response = await sniffing_socket_.ReceiveAsync();
                        if (response.Buffer.Length == 9 && response.Buffer.SequenceEqual(a2s_query_challange_prefix_))
                        {
                            var challange = new ArraySegment<byte>(response.Buffer, response.Buffer.Length - 4, 4);
                            await sniffing_socket_.SendAsync(a2s_query_data_.Concat(challange).ToArray(), response.RemoteEndPoint);
                        }
                    }
                }
            );
        }

        public int GetSocketPort()
        {
            return ((IPEndPoint?)sniffing_socket_.Client.LocalEndPoint)?.Port ?? 0;
        }

        public async Task<int> QueryServerName(IPEndPoint endpoint, CancellationToken cancellation_token = default)
        {
            return await sniffing_socket_.SendAsync(a2s_query_data_, endpoint, cancellation_token);
        }
    }
}
