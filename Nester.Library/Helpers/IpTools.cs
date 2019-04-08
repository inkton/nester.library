using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Inkton.Nester.Helpers
{
    public class IpTools
    {
        public static async Task<string> GetIPAsync(string host)
        {
            string ip = null;

            try
            {
                IPAddress[] ipAddress = await Dns.GetHostAddressesAsync(host);
                ip = ipAddress[0].MapToIPv4().ToString();
                return ip;
            }
            catch (Exception) { }

            return ip;
        }
    }
}
