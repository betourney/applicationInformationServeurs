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
using OLEPRNLib;

namespace infoServeurs
{
    class Program
    {
        // retourne le status du switch
        static public string getStatusSwitch(string ipAdress)
        {
            SNMP snmp;
            int DeviceId = 1;
            int retries = 1;
            int TimeoutInMS = 20000;
            string Result1Str; string status = "";
            try
            {
                snmp = new SNMP();
                snmp.Open(ipAdress, "public", retries, TimeoutInMS);//.1.3.6.1.2.1.2.2.1.7.25
                uint nbPort = snmp.GetAsByte(String.Format(".1.3.6.1.2.1.2.1.0"));
                snmp.Close();
                System.Console.WriteLine("nombre de port : " + nbPort);

                for (int i = 1; i <= nbPort; i++)
                {
                    snmp = new SNMP();
                    snmp.Open(ipAdress, "public", retries, TimeoutInMS);//.1.3.6.1.2.1.2.2.1.7.25//.1.3.6.1.2.1.2.1.0//.1.3.6.1.2.1.2.2.1.6.
                    string adressResult = snmp.Get(".1.3.6.1.2.1.2.2.1.6."+i);
                    byte[] ba = Encoding.Default.GetBytes(adressResult);
                    string hexString = BitConverter.ToString(ba);
                    uint statusResult = snmp.GetAsByte(".1.3.6.1.2.1.2.2.1.8."+i);

                    switch (statusResult)
                    {
                        case 1:
                            Result1Str = "up";
                            break;
                        case 2:
                            Result1Str = "down";
                            break;
                        case 3:
                            Result1Str = "testing";
                            break;
                        case 4:
                            Result1Str = "unknown";
                            break;
                        case 5:
                            Result1Str = "dormant";
                            break;
                        case 6:
                            Result1Str = "notPresent";
                            break;
                        case 7:
                            Result1Str = "lowerLayerDown";
                            break;
                        default:
                            Result1Str = "code inconnu" + statusResult;
                            break;
                    }
                    Console.WriteLine("  port "+i+ " adresse : "+ hexString+" "+ statusResult+" "+Result1Str);
                    snmp.Close();
                 }
            }
            catch (Exception)
            {
                status = "Informations non disponibles...";
            }
            return status;
        }

        // retourne le status de l'imprimante
        static public string getStatus(string ipAdress)
        {
            SNMP snmp;
            int DeviceId = 1;
            int retries = 1;
            int TimeoutInMS = 20000;
            string Result1Str; string status;
            try
            {
                string[] ErrorMessageText = new string[8];
                ErrorMessageText[0] = "service recquis";
                ErrorMessageText[1] = "Eteinte";
                ErrorMessageText[2] = "Bourrage papier";
                ErrorMessageText[3] = "porte ouverte";
                ErrorMessageText[4] = "pas de toner";
                ErrorMessageText[5] = "niveau toner bas";
                ErrorMessageText[6] = "plus de papier";
                ErrorMessageText[7] = "niveau de papier bas";

                snmp = new SNMP();
                snmp.Open(ipAdress, "public", retries, TimeoutInMS);
                uint WarningErrorBits = snmp.GetAsByte(String.Format("25.3.5.1.2.{0}",
                                                       DeviceId));
                uint statusResult = snmp.GetAsByte(String.Format("25.3.2.1.5.{0}",
                                                  DeviceId));

                switch (statusResult)
                {
                    case 2:
                        Result1Str = "OK";
                        break;
                    case 3:
                        Result1Str = "Avertissement: ";
                        break;
                    case 4:
                        Result1Str = "Test: ";
                        break;
                    case 5:
                        Result1Str = "Hors de fonctionnement: ";
                        break;
                    default:
                        Result1Str = "Code Inconnu: " + statusResult;
                        break;
                }
                string Str = "";

                if ((statusResult == 3 || statusResult == 5))
                {
                    int Mask = 1;
                    int NumMsg = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        if ((WarningErrorBits & Mask) == Mask)
                        {
                            if (Str.Length > 0)
                                Str += ", ";
                            Str += ErrorMessageText[i];
                            NumMsg = NumMsg + 1;
                        }
                        Mask = Mask * 2;
                    }
                }
                status = Result1Str + Str;
                snmp.Close();
            }
            catch (Exception)
            {
                status = "Informations non disponibles...";
            }
            return status;
        }

        //retourne le pourcentage d'encre présente dans x toner
        static public string getTonerStatus(string ipAdress, string printerName, int tonerNumber)
        {
            int retries = 1;
            int TimeoutInMS = 20000;
            int tonerNumberDell = 0;
            string status;
            uint currentlevel;
            uint maxlevel;
            try
            {
                SNMP snmp = new SNMP();
                snmp.Open(ipAdress, "public",retries, TimeoutInMS);
                switch (tonerNumber)
                {
                    case 1:
                        tonerNumberDell = 4;
                        break;
                    case 2:
                        tonerNumberDell = 3;
                        break;
                    case 3:
                        tonerNumberDell = 1;
                        break;
                    case 4:
                        tonerNumberDell = 2;
                        break;
                }
                switch (printerName)
                {
                    case "Dell 3010 cn":
                        currentlevel =
         Convert.ToUInt32(snmp.Get(".1.3.6.1.2.1.43.11.1.1.9.1." +
                          tonerNumberDell.ToString()));
                        maxlevel =
         Convert.ToUInt32(snmp.Get(".1.3.6.1.2.1.43.11.1.1.8.1." +
                          tonerNumberDell.ToString()));
                        break;
                    default:
                        currentlevel =
                     Convert.ToUInt32(snmp.Get(".1.3.6.1.2.1.43.11.1.1.9.1." +
                                      tonerNumber.ToString()));
                        maxlevel =
                     Convert.ToUInt32(snmp.Get(".1.3.6.1.2.1.43.11.1.1.8.1." +
                                      tonerNumber.ToString()));
                        break;
                }
                uint remaininglevel = (currentlevel * 100 / maxlevel);
                status = remaininglevel.ToString();
                snmp.Close();
            }
            catch (Exception)
            {
                status = "Informations non disponibles...";
            }
            return status;
        }

        //fonction d'envoi de données au web service; affiche la réponse de la requete http dans la console
        public static bool SendDataToServ(string data)
        {
            string sURL;
            sURL = @"http://10.26.204.8/wsinfserv/index.php/recup" + data;
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
        //recup de fichier sur le serveur linux samba
        public class NetworkConnection : IDisposable
        {
            #region Variables

            /// <summary>
            /// The full path of the directory.
            /// </summary>
            private readonly string _networkName;

            #endregion

            #region Constructors

            /// <summary>
            /// Initializes a new instance of the <see cref="NetworkConnection"/> class.
            /// </summary>
            /// <param name="networkName">
            /// The full path of the network share.
            /// </param>
            /// <param name="credentials">
            /// The credentials to use when connecting to the network share.
            /// </param>
            public NetworkConnection(string networkName, NetworkCredential credentials)
            {
                _networkName = networkName;

                var netResource = new NetResource
                {
                    Scope = ResourceScope.GlobalNetwork,
                    ResourceType = ResourceType.Disk,
                    DisplayType = ResourceDisplaytype.Share,
                    RemoteName = networkName.TrimEnd('\\')
                };

                var result = WNetAddConnection2(
                    netResource, credentials.Password, credentials.UserName, 0);

                if (result != 0)
                {
                    throw new Win32Exception(result);
                }
            }

            #endregion

            #region Events

            /// <summary>
            /// Occurs when this instance has been disposed.
            /// </summary>
            public event EventHandler<EventArgs> Disposed;

            #endregion

            #region Public methods

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            #endregion

            #region Protected methods

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    var handler = Disposed;
                    if (handler != null)
                        handler(this, EventArgs.Empty);
                }

                WNetCancelConnection2(_networkName, 0, true);
            }

            #endregion

            #region Private static methods

            /// <summary>
            ///The WNetAddConnection2 function makes a connection to a network resource. The function can redirect a local device to the network resource.
            /// </summary>
            /// <param name="netResource">A <see cref="NetResource"/> structure that specifies details of the proposed connection, such as information about the network resource, the local device, and the network resource provider.</param>
            /// <param name="password">The password to use when connecting to the network resource.</param>
            /// <param name="username">The username to use when connecting to the network resource.</param>
            /// <param name="flags">The flags. See http://msdn.microsoft.com/en-us/library/aa385413%28VS.85%29.aspx for more information.</param>
            /// <returns></returns>
            [DllImport("mpr.dll")]
            private static extern int WNetAddConnection2(NetResource netResource,
                                                         string password,
                                                         string username,
                                                         int flags);

            /// <summary>
            /// The WNetCancelConnection2 function cancels an existing network connection. You can also call the function to remove remembered network connections that are not currently connected.
            /// </summary>
            /// <param name="name">Specifies the name of either the redirected local device or the remote network resource to disconnect from.</param>
            /// <param name="flags">Connection type. The following values are defined:
            /// 0: The system does not update information about the connection. If the connection was marked as persistent in the registry, the system continues to restore the connection at the next logon. If the connection was not marked as persistent, the function ignores the setting of the CONNECT_UPDATE_PROFILE flag.
            /// CONNECT_UPDATE_PROFILE: The system updates the user profile with the information that the connection is no longer a persistent one. The system will not restore this connection during subsequent logon operations. (Disconnecting resources using remote names has no effect on persistent connections.)
            /// </param>
            /// <param name="force">Specifies whether the disconnection should occur if there are open files or jobs on the connection. If this parameter is FALSE, the function fails if there are open files or jobs.</param>
            /// <returns></returns>
            [DllImport("mpr.dll")]
            private static extern int WNetCancelConnection2(string name, int flags, bool force);

            #endregion

            /// <summary>
            /// Finalizes an instance of the <see cref="NetworkConnection"/> class.
            /// Allows an <see cref="System.Object"></see> to attempt to free resources and perform other cleanup operations before the <see cref="System.Object"></see> is reclaimed by garbage collection.
            /// </summary>
            ~NetworkConnection()
            {
                Dispose(false);
            }
        }

        //retourne le nom ,la capactite et l'espace libre des disques durs de chaque serveurs
        public static void DisqueDurServeur(ManagementScope scope)
        {
            #region HD
            string reponse = "";
            //declaration des objets
            ManagementPath mgmtPath = new ManagementPath("Win32_LogicalDisk");
            ManagementClass classObj = new ManagementClass(null, mgmtPath, null);

            //declaration de la requete : retourne le nom du disque dur du serveur
            SelectQuery requeteLogicalDisk = new SelectQuery();
            requeteLogicalDisk.QueryString = "SELECT * FROM Win32_LogicalDisk where DriveType = 3";

            ManagementObjectSearcher mosLogicalDisk = new ManagementObjectSearcher(scope, requeteLogicalDisk);
            ManagementObjectCollection mocLogicalDisk = mosLogicalDisk.Get();

            foreach (ManagementObject moLogicalDisk in mocLogicalDisk)
            {
                reponse = moLogicalDisk["Name"].ToString() + "_" + moLogicalDisk["FreeSpace"].ToString() + "_" + moLogicalDisk["Size"].ToString();
                Console.WriteLine(reponse);

                //SendDataToServ(reponse);
                //exception pour les disque l'envoi se fera a partir d'ici
            }
            #endregion
        }

        //retourne l'etat des services de chaque serveurs
        public static string ServiceServeur(ManagementScope scope)
        {
            #region Etat des services
            string valueEnvoi = "";
            //declaration des objets
            ManagementPath mgmtPathService = new ManagementPath("Win32_Service");
            ManagementClass classObjService = new ManagementClass(null, mgmtPathService, null);

            //declaration de la requete : retourne le nom du disque dur du serveur
            SelectQuery requeteService = new SelectQuery();
            requeteService.QueryString = "SELECT * FROM Win32_Service";

            ManagementObjectSearcher mosService = new ManagementObjectSearcher(scope, requeteService);
            ManagementObjectCollection mocService = mosService.Get();

            foreach (ManagementObject moService in mocService)
            {
                string nomService = moService["Name"].ToString();
                if (nomService == "Apache2.2" || nomService == "DHCPServer" || nomService == "MySQL")
                {
                    if (moService["ErrorControl"].ToString() == "Normal")
                    {
                        valueEnvoi += "_" + "1";
                    }
                    else
                    {
                        valueEnvoi += "_" + "0";
                    }
                }
            }
            Console.WriteLine(valueEnvoi);
            return valueEnvoi;
            #endregion
        }

        //le retourne True si le serveur passe en parametre peut-etre "pinger" sinon retourne false
        public static bool Ping(string address)
        {
            bool pingOk = false;
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PingStatus where address = '" + address + "''");
                foreach (ManagementObject queryObj in searcher.Get())
                {
                    if (queryObj["StatusCode"].ToString() != null)
                    {
                        pingOk = true;
                        break;
                    }
                }
            }
            catch (ManagementException e)
            {
                Console.WriteLine("An error occurred while querying for WMI data: " + e.Message);
            }
            return pingOk;
        }

        #region Objects needed for the Win32 functions
#pragma warning disable 1591

        /// <summary>
        /// The net resource.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public class NetResource
        {
            public ResourceScope Scope;
            public ResourceType ResourceType;
            public ResourceDisplaytype DisplayType;
            public int Usage;
            public string LocalName;
            public string RemoteName;
            public string Comment;
            public string Provider;
        }

        /// <summary>
        /// The resource scope.
        /// </summary>
        public enum ResourceScope
        {
            Connected = 1,
            GlobalNetwork,
            Remembered,
            Recent,
            Context
        };

        /// <summary>
        /// The resource type.
        /// </summary>
        public enum ResourceType
        {
            Any = 0,
            Disk = 1,
            Print = 2,
            Reserved = 8,
        }

        /// <summary>
        /// The resource displaytype.
        /// </summary>
        public enum ResourceDisplaytype
        {
            Generic = 0x0,
            Domain = 0x01,
            Server = 0x02,
            Share = 0x03,
            File = 0x04,
            Group = 0x05,
            Network = 0x06,
            Root = 0x07,
            Shareadmin = 0x08,
            Directory = 0x09,
            Tree = 0x0a,
            Ndscontainer = 0x0b
        }
#pragma warning restore 1591
        #endregion

        public static void recuperationFichierLinux(string path, string user, string password, List<String> typeExtension)
        {
            using (new NetworkConnection(path, new NetworkCredential(user, password)))
            {
                //File.ReadLines(@"\\10.26.204.253\Web\index.php");
                //Console.Write(File.ReadLines(@"\\10.26.204.253\Web\index.php"));
                string[] extensions = { "*.mp3", "*.mp4", "*.avi" };//a recup du fichier config A faire
                foreach (string extension in extensions)
                {
                    try
                    {
                        foreach (string searchfile in Directory.EnumerateFiles(path, extension, SearchOption.AllDirectories))
                        {
                            Console.WriteLine(searchfile);
                            //a envoyer via requete recupfile/
                        }
                    }
                    catch
                    {
                        Console.WriteLine("prob acces sur " + extension);
                    }
                }
            }
        }//rajouter la methode 

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

        public static string readConfig(string info)
        {

            int counter = 0;
            string line;
            List<string> contenuConfig = new List<string>();

            List<string> adressServeur = new List<string>();
            List<string> adressSwitch = new List<string>();
            List<string> adressImprimante = new List<string>();
            List<string> ExtensionFichier = new List<string>();
            List<string> listeUserMdp = new List<string>();
            List<string> listePathFichier = new List<string>();
            // Read the file and display it line by line.
            StreamReader file = new StreamReader(@"c:\test.txt");
            while ((line = file.ReadLine()) != null)
            {
                contenuConfig.Add(line);
            }
            file.Close();
            foreach (string value in contenuConfig)
            {
                if (true)
                {

                }
            }
        }

        static void Main(string[] args)
        {

            WebProxy myProxy = new WebProxy("http://10.254.4.1", 80);//activer le proxy
            myProxy.BypassProxyOnLocal = true;

            #region lecture des adresse ip à scanner dans le .conf
            string ipStart = "";
            string line;
            // Read the file and display it line by line.
            StreamReader file = new StreamReader(@"test.conf");
            Console.WriteLine("contenu du fichier conf: ");
            while ((line = file.ReadLine()) != null)
            {
                Console.WriteLine("\n"+line);
                ipStart = ipStart + line;
            }
            file.Close();
            #endregion

            #region connection au serveur avec ManagementScope et WMI
            //--------------Connection Serveur-----------------//
            //information pour ce connecter au serveur
            string sUserName = "admini";
            string sPwd = "6gdp9";
            ConnectionOptions opt = new ConnectionOptions();
            opt.Authority = "ntlmdomain:";
            opt.Username = sUserName;
            opt.Password = sPwd;
            // connection au serveur
            ManagementScope scope = new ManagementScope(@"\\10.26.204.1\root\cimv2", opt);// |-> recherche le serveur - remplacer par la recherche d'id                                                                           // si l'adresse change recuperer le mot de passe et l'identifiant
            scope.Connect();
            #endregion

            #region Affiche les stats du switch
            //info switch
            string switchstat = getStatusSwitch("10.66.202.1");
            System.Console.WriteLine("Switch : " + switchstat); 
            #endregion

            #region Affiche l'etat de l'imprimante et ses toners
            //info imprimante 
            string printerstat = getStatus("10.26.205.5");
            string tonerstat = getTonerStatus("10.26.205.5", "SD", 1);
            System.Console.WriteLine("Etat imprimante : " + printerstat + " toner " + tonerstat + "%");
            #endregion

            #region affiche les dossiers recuperer sur le dossier linux
            List<string> lesExtension = new List<string>();
            recuperationFichierLinux(@"\\10.26.204.253\Web\", "formateur", "centrenord",lesExtension);//devra chercher dans le fichiers config
            #endregion

            #region affiche des info sur les disques dur du serveur
            DisqueDurServeur(scope);
            #endregion

            #region affiche l'etat des Services du Serveur
            ServiceServeur(scope); 
            #endregion

            #region test ping des ip chargé et envoi au web serv
            string[] ipS = ipStart.Split('_');
            StreamWriter filew = new System.IO.StreamWriter("test.txt", true);
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
                        SendDataToServ("info/" + ip + "_" + statusping + "_1_1_1_1_1");
                        Console.WriteLine("envoi au web service ok");
                    }
                    catch //en cas d'echec on enregistre la requete dans un fichier
                    {
                        Console.WriteLine("info/" + ip + "envoi au web service echec");
                        filew.WriteLine(ip + "_" + statusping + "_0_0_0_0_0");
                    }
                    file.Close();
                    Console.ReadLine();
                }
                catch
                {
                    file.Close();
                    Console.WriteLine("pas de connection");
                    Console.ReadLine();
                }
                #endregion

            }  
        }
    }
}
