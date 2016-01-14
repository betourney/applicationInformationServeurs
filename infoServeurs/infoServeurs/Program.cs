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
            WebProxy myProxy = new WebProxy("http://10.254.4.1", 80);//activer le proxy
            myProxy.BypassProxyOnLocal = true;

            #region lecture des adresse ip à scanner dans le .conf
            string ipStart = "";
            string line;
            // Read the file and display it line by line.
            System.IO.StreamReader file =
                new System.IO.StreamReader(@"test.conf");
            System.Console.WriteLine("contenu du fichier conf: ");
            while ((line = file.ReadLine()) != null)
            {
                System.Console.WriteLine("\n"+line);
                ipStart = ipStart + line;
            }
            file.Close();
            #endregion
            //info switch
            string switchstat = getStatusSwitch("10.26.202.15");
            System.Console.WriteLine("Switch : " + switchstat);

            #region Affiche l'etat de l'imprimante et ses toners
            //info imprimante 
            string printerstat = getStatus("10.26.205.5");
            string tonerstat = getTonerStatus("10.26.205.5", "SD", 1);
            System.Console.WriteLine("Etat imprimante : " + printerstat +" toner "+ tonerstat+"%");
            // 
            #endregion

            #region test ping des ip chargé et envoi au web serv
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
                    Console.WriteLine("pas de connection");
                    Console.ReadLine();
                } 
                #endregion
            }
        }
    }
}
