using System;
using System.Net;
using System.Net.Sockets;
using System.Web;
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
using System.IO;

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
        //fonction d'envoi de données au web service; affiche la réponse de la requete http dans la console
        public static bool SendDataToServ(string data)
        {
            string sURL;
            sURL = @"http://10.26.204.8/wsinfserv/index.php/recup/" + data;
            //http://www.microsoft.com http://10.26.204.8/wsinfserv
            try
            {
                WebRequest wrGETURL;
                wrGETURL = WebRequest.Create(sURL);
                wrGETURL.Proxy = null;//by pass le proxy

                //WebProxy myProxy = new WebProxy("http://10.254.4.1", 80);//activer le proxy
                //myProxy.BypassProxyOnLocal = true; 

                Stream objStream;
                objStream = wrGETURL.GetResponse().GetResponseStream();

                StreamReader objReader = new StreamReader(objStream);

                string sLine = "";
                int i = 0;

                while (sLine != null)
                {
                    i++;
                    sLine = objReader.ReadLine();
                    if (sLine != null)
                        Console.WriteLine("{0}:{1}", i, sLine);
                }
                //Console.ReadLine();
                return true;
            }
            catch
            {
                return false;
            }
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
            string ipStart = "";
            string line;

            // Read the file and display it line by line.
            System.IO.StreamReader file =
                new System.IO.StreamReader(@"test.conf");
            while ((line = file.ReadLine()) != null)
            {
                System.Console.WriteLine(line);
                ipStart = ipStart + line;
            }
            file.Close();


            string[] ipS = ipStart.Split('_');
            System.IO.StreamWriter filew = new System.IO.StreamWriter("test.txt", true);
            foreach (string ip in ipS)
            {
                Ping pingSender = new Ping();
                PingOptions options = new PingOptions();
                options.DontFragment = true;

                // Create a buffer of 32 bytes of data to be transmitted.
                string data = "aaaa";
                byte[] buffer = Encoding.ASCII.GetBytes(data);
                int timeout = 120;
                try
                {
                    PingReply reply = pingSender.Send(ip, timeout, buffer, options);
                    int statusping;
                    if (reply.Status == IPStatus.Success)
                    {
                        Console.WriteLine(ip + " : ok");
                        statusping = 1;
                    }
                    else
                    {
                        Console.WriteLine(ip + " : echec");
                        statusping = 0;
                    }
                    try //la requete est correctement envoyé au serveur
                    {
                        SendDataToServ(ip + "_" + statusping + "_1_1_1_1_1");
                        Console.WriteLine("envoi au web service ok");
                    }
                    catch //en cas d'echec on enregistre la requete dans un fichier
                    {
                        Console.WriteLine(ip + "envoi au web service echec");
                        filew.WriteLine(ip + "_" + statusping + "_0_0_0_0_0");
                    }
                    file.Close();
                    Console.ReadLine();
                }
                #region MyRegion catch pas de co
                catch
                {
                    Console.WriteLine("pas de connection");
                    Console.ReadLine();
                }
                #endregion

            }
        }
    }
}
