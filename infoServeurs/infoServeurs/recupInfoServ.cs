﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace essaiRecupDonneesReseaux
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

        //methodes
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
        public static IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOpt)
        {
            try
            {
                var dirFiles = Enumerable.Empty<string>();
                if (searchOpt == SearchOption.AllDirectories)
                {
                    dirFiles = Directory.EnumerateDirectories(path).SelectMany(x => EnumerateFiles(x, searchPattern, searchOpt));
                }
                return dirFiles.Concat(Directory.EnumerateFiles(path, searchPattern));
            }
            catch (UnauthorizedAccessException)
            {
                return Enumerable.Empty<string>();
            }
        }
        public static bool SendDataToServ(string data)
        {
            string sURL;
            sURL = @"http://10.26.204.8/wsinfserv/index.php/recup/" + data;
            //http://www.microsoft.com http://10.26.204.8/wsinfserv
            try
            {
                WebRequest wrGETURL;

                wrGETURL = WebRequest.Create(sURL);

                WebProxy myProxy = new WebProxy("http://10.254.4.1", 80);//by pass le proxy
                myProxy.BypassProxyOnLocal = true;
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
                Console.ReadLine();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void second(string[] args)
        #region Main
        //public static void Main(string[] args)
        {
            try
            {
                string valueEnvoi = "10.26.204.1";

                #region Information du pc en local
                Console.WriteLine("--------------Information PC-----------------");
                Console.WriteLine(PingIP);
                Console.WriteLine("UserName:{0}", Environment.UserDomainName);
                var userIp = Dns.GetHostEntry(Environment.UserDomainName).AddressList[2].ToString();
                Console.WriteLine(userIp);
                Console.ReadLine();
                #endregion

                #region liste des machines sur le serveurs
                /*
                Console.WriteLine("--------------Liste des machines sur le serveurs-----------------");
                string[] computers = GetComputers();
                foreach (string computer in computers)
                {
                    Console.WriteLine(computer);
                }
                Console.ReadLine();
                */
                #endregion

                #region connection au serveur
                //--------------Connection Serveur-----------------//
                //information pour ce connecter au serveur
                string sUserName = "admini";
                string sPwd = "6gdp9";
                ConnectionOptions opt = new ConnectionOptions();
                opt.Authority = "ntlmdomain:" + userIp;
                opt.Username = sUserName;
                opt.Password = sPwd;
                // connection au serveur
                ManagementScope scope = new ManagementScope(@"\\10.26.204.1\root\cimv2", opt);// |-> recherche le serveur - remplacer par la recherche d'id
                                                                                              // si l'adresse change recuperer le mot de passe et l'identifiant
                scope.Connect();
                #endregion

                #region Liste des service du serveurs
                /*
                Console.WriteLine("--------------Liste des services du serveurs-----------------");
                //declaration des objets
                ManagementPath mgmtPathProcess = new ManagementPath("Win32_Process");
                ManagementClass classObjProcess = new ManagementClass(null, mgmtPathProcess, null);

                //declaration de la requete : retourne le nom du disque dur du serveur
                SelectQuery requeteProcess = new SelectQuery();
                requeteProcess.QueryString = "SELECT * FROM Win32_Process";

                ManagementObjectSearcher mosProcess = new ManagementObjectSearcher(scope, requeteProcess);
                ManagementObjectCollection mocProcess = mosProcess.Get();

                foreach (ManagementObject moProcess in mocProcess)
                {
                    string serviceServeurs = moProcess["Caption"].ToString();
                    Console.WriteLine(serviceServeurs);
                }
                Console.ReadLine();
                */
                #endregion

                Console.WriteLine("--------------Win32_services -> Caption (properties)-----------------");
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
                            Console.WriteLine(valueEnvoi);
                        }
                        else
                        {
                            valueEnvoi += "_" + "0";
                            Console.WriteLine(valueEnvoi);
                        }
                    }
                }
                valueEnvoi += "_1_0_1";
                Console.WriteLine(valueEnvoi);
                SendDataToServ(valueEnvoi);

            }
            #region Methode split
            /*
            Console.WriteLine("-------------- utilisation de la methode split-----------------");

            //char[] delimiterChars = { ' ', ',', '.', ':', '\t' };  |-> defini les delimiter utiliser exemple => , ou ; ou . ou etc..
            char[] delimiterChars = { ';' };
            string text = "one;two;three;four;five;six;seven";
            Console.WriteLine("Original text: '{0}'", text);

            string[] words = text.Split(delimiterChars);
            Console.WriteLine("{0} words in text:", words.Length);

            foreach (string s in words)
            {
                System.Console.WriteLine(s);
            }
            Console.ReadLine();
            #endregion

            #region lecture fichier
            Console.WriteLine("--------------lecture dans un fichier-----------------");

            String line;
            try
            {
                //Pass the file path and file name to the StreamReader constructor 
                StreamReader streamR = new StreamReader("C:\\Users\\yoann\\Documents\\exemple.txt");

                //Lire la premiere ligne du fichier Sample.txt 
                line = streamR.ReadLine();

                //Continuer la lecture jusqu'a la fin du fichier 
                while (line != null)
                {
                    char delimitChars = ';';
                    Console.WriteLine("Original text: '{0}'", line);
                    string[] mot = text.Split(delimitChars);

                    foreach (string value in mot)
                    {
                        Console.WriteLine(value);
                    }

                    //write the lie to console window 
                    Console.WriteLine(line);
                    //lecture du ligne du texte 
                    line = streamR.ReadLine();
                }

                //Fermiture du fichier 
                streamR.Close();
                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
            }
            finally
            {
                Console.WriteLine("Executing finally block.");
            }
            System.Console.ReadKey();
            #endregion

            #region HD
            Console.WriteLine("--------------Nom , Capaciter et espace des disques du serveur-----------------");
            //declaration des objets
            ManagementPath mgmtPath = new ManagementPath("Win32_LogicalDisk");
            ManagementClass classObj = new ManagementClass(null, mgmtPath, null);

            //declaration de la requete : retourne le nom du disque dur du serveur
            SelectQuery requeteLogicalDisk = new SelectQuery();
            requeteLogicalDisk.QueryString = "SELECT * FROM Win32_LogicalDisk";

            ManagementObjectSearcher mosLogicalDisk = new ManagementObjectSearcher(scope, requeteLogicalDisk);
            ManagementObjectCollection mocLogicalDisk = mosLogicalDisk.Get();

            foreach (ManagementObject moLogicalDisk in mocLogicalDisk)
            {
                Console.WriteLine("Nom : " + moLogicalDisk["Name"].ToString() + " Espace libre : " + moLogicalDisk["FreeSpace"].ToString() + " Capaciter : " + moLogicalDisk["Size"].ToString());
            }
            Console.ReadLine();
        }
        */
            #endregion

            //affiche les messages d'erreur
            #region BLOC_CATCH
            catch (ManagementException Ex)
            {
                Console.WriteLine(String.Format("erreur : " + Ex.Message));
                Console.ReadLine();
            }
            catch (Exception Ex)
            {
                Console.WriteLine(String.Format("erreur : " + Ex.Message));
                Console.ReadLine();
            }
            #endregion
        }
    }
    #endregion
}


