using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Threading;
using System.Management;
using System.Management.Instrumentation;
using System.Runtime.InteropServices;
using System.Security;
using System.Net.NetworkInformation;
using System.Collections;

namespace infoServeurs
{
    class Program
    {

        [StructLayout(LayoutKind.Sequential)]
        public struct _SERVER_INFO_100
        {
            internal int sv100_platform_id;
            [MarshalAs(UnmanagedType.LPWStr)]
            internal string sv100_name;
        }

        [DllImport("Netapi32", SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
        public static extern int NetApiBufferFree(IntPtr pBuf);

        [DllImport("Netapi32", CharSet = CharSet.Auto, SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
        public static extern int NetServerEnum(
            string ServerNane,
            int dwLevel,
            ref IntPtr pBuf,
            int dwPrefMaxLen,
            out int dwEntriesRead,
            out int dwTotalEntries,
            int dwServerType,
            string domain,
            out int dwResumeHandle
            );

        const int LVL_100 = 100;
        const int MAX_PREFERRED_LENGTH = -1;
        const int SV_TYPE_WORKSTATION = 1;
        const int SV_TYPE_SERVER = 2;
 
        public static string[] GetComputers()
        {
            ArrayList computers = new ArrayList();
            IntPtr buffer = IntPtr.Zero, tmpBuffer = IntPtr.Zero;
            int entriesRead, totalEntries, resHandle;
            int sizeofINFO = Marshal.SizeOf(typeof(_SERVER_INFO_100));

            try
            {
                int ret = NetServerEnum(null, LVL_100, ref buffer, MAX_PREFERRED_LENGTH, out entriesRead, out totalEntries, SV_TYPE_WORKSTATION | SV_TYPE_SERVER, null, out resHandle);
                if (ret == 0)
                {
                    for (int i = 0; i < totalEntries; i++)
                    {
                        tmpBuffer = new IntPtr((int)buffer + (i * sizeofINFO));

                        _SERVER_INFO_100 svrInfo = (_SERVER_INFO_100)Marshal.PtrToStructure(tmpBuffer, typeof(_SERVER_INFO_100));
                        computers.Add(svrInfo.sv100_name);
                    }
                }
                else
                    throw new Win32Exception(ret);
            }
            finally
            {
                NetApiBufferFree(buffer);
            }

            return (string[])computers.ToArray(typeof(string));
        }

        //
        public static bool PingIP
        {
            get
            {
                Uri url = new Uri("http://10.26.204.1");
                string pingurl = string.Format("{0}", url.Host);
                string host = pingurl;
                bool result = false;
                Ping p = new Ping();
                try
                {
                    PingReply reply = p.Send(host, 3000);
                    if (reply.Status == IPStatus.Success)
                        return true;
                }
                catch { }
                return result;
            }
        }
        //

        static void Main(string[] args)
        {
            Console.WriteLine(PingIP);
            Console.WriteLine("UserName:{0}", Environment.UserDomainName);
            var userIp = Dns.GetHostEntry(Environment.UserDomainName).AddressList[1].ToString();
            Console.WriteLine(userIp);
            Console.ReadLine();

            // Write the string to a file.
            System.IO.StreamWriter file = new System.IO.StreamWriter("test.txt",true);
            file.WriteLine("User Domain name: {0} User IP: {1}", Environment.UserDomainName, userIp);
            string[] computers = GetComputers();

            foreach (string computer in computers)
            {
                Console.WriteLine(computer);
                file.WriteLine("Computer : {0} \n", computer);
            }
            Console.ReadLine();

            file.Close();

        }
    }
}
