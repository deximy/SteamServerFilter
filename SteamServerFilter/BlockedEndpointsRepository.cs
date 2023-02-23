using System.Net;

namespace SteamServerFilter
{
    public class BlockedEndpointsRepository
    {
        private readonly List<IPEndPoint> blocked_endpoints_list_;

        public BlockedEndpointsRepository()
        {
            blocked_endpoints_list_ = new List<IPEndPoint>();
        }

        public IReadOnlyCollection<IPEndPoint> Get()
        {
            return blocked_endpoints_list_;
        }

        public void Add(IPEndPoint endpoint)
        {
            if (!blocked_endpoints_list_.Contains(endpoint))
            {
                blocked_endpoints_list_.Add(endpoint);
            }
        }

        public void Add(IPAddress address, ushort port)
        {
            Add(new(address, port));
        }
    }
}
