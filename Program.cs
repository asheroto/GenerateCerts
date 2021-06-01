using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;

namespace GenerateCerts
{
    internal class Program
    {
        private static string openssl_manual_path;
        private static string destdir;
        private static string slash;

        private static string OutputDirectoryName = "SSL_Certs_Out";

        private const string CACertName = "ca";
        private const string CertificateCertName = "certificate";
        private const string ServerCertName = "server";
        private const string ClientCertName = "client";
        private const string CertValidityDays = "3650";

        private const string GenerateCertsSerial = "GenerateCerts-serial";
        private const string GenerateCertsDb = "GenerateCerts-db";
        private const string GenerateCertsCaConf = "GenerateCerts-ca.conf";
        private const string GenerateCertsCertsConf = "GenerateCerts-certs.conf";

        private const string vcredist32_url = "https://github.com/asheroto/GenerateCerts/releases/download/OpenSSL-1.1.1k/VC_redist.x86.exe";
        private const string vcredist64_url = "https://github.com/asheroto/GenerateCerts/releases/download/OpenSSL-1.1.1k/VC_redist.x64.exe";
        private const string openssl32_url = "https://github.com/asheroto/GenerateCerts/releases/download/OpenSSL-1.1.1k/Win32OpenSSL-1_1_1k.exe";
        private const string openssl64_url = "https://github.com/asheroto/GenerateCerts/releases/download/OpenSSL-1.1.1k/Win64OpenSSL-1_1_1k.exe";

        private static void Main()
        {
            // Set title
            Console.Title = Assembly.GetExecutingAssembly().GetName().Name;

            // Set slash depending on OS
            if (GetOS() == OSPlatform.Windows)
                slash = @"\";
            else
                slash = @"/";

            // Set destination directory
            destdir = Environment.CurrentDirectory + slash + OutputDirectoryName;

            Console.WriteLine();

        retry_openssl_test:
            try
            {
                // Confirm openssl opens
                Process o = new Process();
                if (openssl_manual_path == null)
                    o.StartInfo.FileName = "openssl";
                else
                    o.StartInfo.FileName = openssl_manual_path;
                o.StartInfo.Arguments = "version";
                o.StartInfo.UseShellExecute = false;
                o.StartInfo.CreateNoWindow = true;
                o.StartInfo.RedirectStandardOutput = true;
                o.Start();
            }
            catch (Exception)
            {
                if (GetOS() != OSPlatform.Windows)
                {
                    // Non-Windows
                    Console.WriteLine("Could not detect OpenSSL. Please install it first and then run GenerateCerts.");
                    Console.WriteLine();
                    Console.WriteLine("Try this command:");
                    Console.WriteLine("    apt update && apt install openssl -y");
                    Console.WriteLine("or try:");
                    Console.WriteLine("    yum install openssl");
                    Console.WriteLine();
                    Console.WriteLine("If that does not work, try the instructions here:");
                    Console.WriteLine("  http://bit.ly/linux-openssl");
                    Console.ReadLine();
                    System.Environment.Exit(0);
                }

                // Windows
                Console.WriteLine("Could not detect OpenSSL. What would you like to do?");
                Console.WriteLine("(1) Download and install OpenSSL for me");
                Console.WriteLine("(2) Specify a path to OpenSSL.exe");
                Console.WriteLine();
                string todo = Console.ReadLine();
                Console.WriteLine();

                if (todo == "1")
                {
                    // Set temp file path
                    var temp_openssl = Path.GetTempFileName() + ".exe";
                    var temp_vcredist = Path.GetTempFileName() + ".exe";

                    try
                    {
                        // Download OpenSSL installer

                        Console.WriteLine("Downloading, this should take less than 5 minutes...");

                        WebClient wc = new WebClient();

                        string vcredist_url = null;
                        string openssl_url = null;
                        if (Environment.Is64BitOperatingSystem)
                        {
                            vcredist_url = vcredist64_url;
                            openssl_url = openssl64_url;
                        }
                        else
                        {
                            vcredist_url = vcredist32_url;
                            openssl_url = openssl32_url;
                        }
                        wc.DownloadFile(vcredist_url, temp_vcredist);
                        wc.DownloadFile(openssl_url, temp_openssl);

                        Console.WriteLine("Download complete, installing, this should take less than 5 minutes...");
                    }
                    catch (Exception)
                    {
                        try
                        {
                            // Delete temp files
                            File.Delete(temp_openssl);
                            File.Delete(temp_vcredist);
                        }
                        catch (Exception)
                        {
                        }
                        Console.WriteLine("Error installing OpenSSL. Possible firewall or content filtering issue?");
                        Console.ReadLine();
                        System.Environment.Exit(0);
                    }

                    try
                    {
                        // Install vcredist140
                        Process vcr = new Process();
                        vcr.StartInfo.FileName = temp_vcredist;
                        vcr.StartInfo.Arguments = "/install /quiet /norestart";
                        vcr.Start();
                        while (!vcr.HasExited)
                        {
                        }

                        // Install OpenSSL
                        Process pp = new Process();
                        pp.StartInfo.FileName = temp_openssl;
                        pp.StartInfo.Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-";
                        pp.Start();
                        while (!pp.HasExited)
                        {
                        }

                        string OpenSSL_Dir = null;

                        if (Environment.Is64BitOperatingSystem)
                        {
                            OpenSSL_Dir = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + @"\OpenSSL-Win64\bin";
                        }
                        else
                        {
                            OpenSSL_Dir = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + @"\OpenSSL-Win32\bin";
                        }


                        // Set PATH var on the system so that you can type OpenSSL in command prompt in any directory
                        var p = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine);
                        Environment.SetEnvironmentVariable("PATH", p + ";" + OpenSSL_Dir, EnvironmentVariableTarget.Machine);

                        // Set path to OpenSSL
                        openssl_manual_path = OpenSSL_Dir + @"\OpenSSL.exe";

                        // Delete downloaded file
                        try
                        {
                            // Delete temp files
                            File.Delete(temp_openssl);
                            File.Delete(temp_vcredist);
                        }
                        catch (Exception)
                        {
                        }

                        // Report installed
                        Console.WriteLine("Installation complete, restarting application...");
                        System.Threading.Thread.Sleep(2000);

                        Console.WriteLine();

                        // Retry OpenSSL test
                        goto retry_openssl_test;
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Error installing OpenSSL. Are you running as Administrator?");
                        Console.ReadLine();
                        System.Environment.Exit(0);
                    }
                    goto retry_openssl_test;
                }
                else if (todo == "2")
                {
                retry_specifypath:
                    Console.WriteLine();
                    Console.WriteLine("Please specify path to OpenSSL.exe:");
                    var osp = Console.ReadLine();
                    Console.WriteLine();
                    if (osp == null)
                        goto retry_specifypath;
                    else if (File.Exists(osp))
                    {
                        openssl_manual_path = osp;
                        goto retry_openssl_test;
                    }
                    else
                    {
                        Console.WriteLine("The path you specified does not exist, please try again.");
                        goto retry_specifypath;
                    }
                }
                else
                    goto retry_openssl_test;
            }

            Console.Clear();

            // Information
            Console.WriteLine("################### WARNING ######################");
            Console.WriteLine("GenerateCerts generates self-signed SSL/TLS certificates");
            Console.WriteLine();
            Console.WriteLine("The following folder will be used to store the certificates:");
            Console.WriteLine("    " + destdir);
            Console.WriteLine("Any existing data in the directory shown above will be erased and overwritten after pressing ENTER");
            Console.WriteLine();
            Console.WriteLine("Press Ctrl-C to terminate the app if you do not want the above files to be deleted");
            Console.WriteLine("##################################################");
            Console.WriteLine();
            Console.WriteLine("Press enter to continue");
            Console.ReadLine();
            Console.WriteLine();

            // Delete existing certificates
            ClearOutputFolder();

            // Create folder if needed
            if (Directory.Exists(destdir) == false)
                Directory.CreateDirectory(destdir);

            Console.Clear();

        oneortwo_retry:
            Console.WriteLine();
            Console.WriteLine("Do you want one certificate (certificate.crt) or two certificates (server.crt and client.crt)?");
            Console.WriteLine("A root certificate authority is created either way.");
            Console.WriteLine("(1) one certificates (2) two certificates");
            string oneortwo_var = null;
            string oneortwo_line = Console.ReadLine();
            if (oneortwo_line == "1")
                oneortwo_var = "1";
            else if (oneortwo_line == "2")
                oneortwo_var = "2";
            else
                goto oneortwo_retry;

            Console.WriteLine();
            Console.WriteLine("Please create passwords for your certs or press enter to not set a password");
            Console.WriteLine("Root CA Cert password is REQUIRED if you want PFX and PEM files created (for all files), otherwise optional");
            Console.WriteLine();

            string rootca_pw = null;
            string server_pw = null;
            string client_pw = null;
            string certificate_pw = null;

            Console.Write("Root CA Cert Password: ");
            rootca_pw = Console.ReadLine();
            if (oneortwo_var == "1")
            {
                Console.Write("Certificate Password: ");
                certificate_pw = Console.ReadLine();
            }
            else
            {
                Console.Write("Server Cert Password:  ");
                server_pw = Console.ReadLine();
                Console.Write("Client Cert Password:  ");
                client_pw = Console.ReadLine();
            }

        subject_retry:
            Console.WriteLine();
            Console.WriteLine("What is the SUBJECT of the certificate?");
            Console.WriteLine("Usually this is the hostname that you'll connect over, such as localhost or your.domain.com");
            Console.WriteLine("If you want to enter an IP address, wait until the next question");
            Console.WriteLine("Press enter to set to localhost");
            Console.WriteLine();
            string cn = null;
            string subj_var = null;
            string subj_line = Console.ReadLine();
            if (subj_line.Length > 0)
            {
                if (subj_line.Contains(" "))
                {
                    Console.WriteLine("SUBJECT cannot contain a space!");
                    goto subject_retry;
                }
                subj_var = subj_line;
            }
            else
                subj_var = "localhost";
            cn = "CN = " + subj_var;

        san_retry:
            Console.WriteLine();
            Console.WriteLine("What is the SUBJECT ALTERNATIVE NAME (SAN) of the certificate?");
            Console.WriteLine("Usually this is an alternate hostname such as 127.0.0.1 or alt.domain.com");
            Console.WriteLine("If you are using the certificates to connect over an IP address rather than a hostname, enter that IP address here");
            Console.WriteLine("Press enter to skip");
            Console.WriteLine();
            string san = null;
            string san_var = null;
            string san_line = Console.ReadLine();
            if (san_line.Length > 0)
            {
                if (san_line.Contains(" "))
                {
                    Console.WriteLine("SAN cannot contain a space!");
                    goto san_retry;
                }
                try
                {
                    var parsedIP = IPAddress.Parse(san_line);
                    if (parsedIP != null)
                        san_var = "IP.1 = " + parsedIP;
                }
                catch (Exception)
                {
                }
                if (san_var == null)
                    san_var = "DNS.2 = " + san_line;
            }
            san = "DNS.1 = " + subj_var + Environment.NewLine + san_var;

            // Write temp files
            File.WriteAllText(destdir + slash + GenerateCertsSerial, "00" + Environment.NewLine);
            File.WriteAllText(destdir + slash + GenerateCertsDb, "");
            File.WriteAllText(destdir + slash + GenerateCertsCaConf, GetOpenSslCfg("CN = GenerateCerts Root CA", "", "", CACertName, "critical,CA:true", "nonRepudiation, digitalSignature, keyEncipherment, cRLSign, keyCertSign", CertValidityDays));
            File.WriteAllText(destdir + slash + GenerateCertsCertsConf, GetOpenSslCfg(cn, "subjectAltName = @alt_names", san, CACertName, "CA:false", "nonRepudiation, digitalSignature, keyEncipherment", CertValidityDays));

            // Begin writing cert data

            // Root CA Certificate
            GenerateSection("Root CA Certificate");
            GenerateCertificate(CACertName, rootca_pw, rootca_pw, true);

            if (oneortwo_var == "1")
            {
                // One certificate
                GenerateSection("Certificate");
                GenerateCertificate(CertificateCertName, certificate_pw, rootca_pw);
            }
            else if (oneortwo_var == "2")
            {
                // Two certificates

                // Server certificate
                GenerateSection("Server Certificate");
                GenerateCertificate(ServerCertName, server_pw, rootca_pw);

                // Client certificate
                GenerateSection("Client Certificate");
                GenerateCertificate(ClientCertName, client_pw, rootca_pw);
            }

            GenerateSection("RESULTS");
            if (rootca_pw.Length == 0)
            {
                Console.WriteLine("WARNING: Root CA password not specified, PFX and PEM files NOT created");
                Console.WriteLine();
            }
            Console.WriteLine("Certificates have been generated!");
            Console.WriteLine();
            Console.WriteLine("Don't forget the passwords you used for the certificates:");
            Console.WriteLine("Root CA Cert Password = " + rootca_pw);
            if (oneortwo_var == "1")
                Console.WriteLine("Certificate Password  = " + certificate_pw);
            else if (oneortwo_var == "2")
            {
                Console.WriteLine("Server Cert Password  = " + server_pw);
                Console.WriteLine("Client Cert Password  = " + client_pw);
            }
            Console.WriteLine();
            Console.WriteLine("Certificates stored in:");
            Console.WriteLine("    " + destdir);
            Console.WriteLine("##################################################");
            Console.WriteLine();

            Console.WriteLine();

            DeleteTempCertFiles();

            Console.ReadLine();
            System.Environment.Exit(0);
        }

        /// <summary>Runs the openssl process with arguments</summary>
        /// <param name="args"></param>
        private static void openssl(string args)
        {
            try
            {
                Process o = new Process();
                o.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                if (openssl_manual_path == null)
                    o.StartInfo.FileName = "openssl";
                else
                    o.StartInfo.FileName = openssl_manual_path;
                o.StartInfo.Arguments = args;
                if (Directory.Exists(destdir) == false)
                    Directory.CreateDirectory(destdir);
                o.StartInfo.WorkingDirectory = destdir;
                // o.StartInfo.RedirectStandardOutput = True
                // o.StartInfo.RedirectStandardError = True
                o.Start();

                while (o.HasExited == false)
                    System.Threading.Thread.Sleep(50);
            }
            catch (Exception)
            {
                Console.WriteLine("Error launching openssl, are you sure it's set up correctly and accessible from the command line?");
            }
        }

        /// <summary>Clears files in the output folder</summary>
        public static void ClearOutputFolder()
        {
            try
            {
                DirectoryInfo f = new DirectoryInfo(destdir);
                FileInfo[] fiArr = f.GetFiles();
                foreach (var fri in fiArr)
                {
                    try
                    {
                        fri.Delete();
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>Deletes temporary certificate files</summary>
        private static void DeleteTempCertFiles()
        {
            string[] certfiles = new[] {
            "00.pem",
            "01.pem",
            "02.pem",
            "03.pem",
            "04.pem",
            "05.pem",
            GenerateCertsDb,
            GenerateCertsDb + ".attr",
            GenerateCertsDb + ".attr.old",
            GenerateCertsDb + ".old",
            GenerateCertsDb + ".new",
            GenerateCertsDb + ".attr.new",
            GenerateCertsSerial,
            GenerateCertsSerial + ".old",
            GenerateCertsSerial + ".new",
            GenerateCertsCaConf,
            GenerateCertsCertsConf
        };

            DeleteIfExists(certfiles);
        }

        /// <summary>Deletes if exists in array</summary>
        /// <param name="path"></param>
        public static void DeleteIfExists(string[] path)
        {
            foreach (string p in path)
            {
                if (File.Exists(destdir + slash + p))
                {
                    try
                    {
                        File.Delete(destdir + slash + p);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        public static void GenerateSection(string title)
        {
            Console.WriteLine();
            Console.WriteLine("##################################################");
            Console.WriteLine(title);
            Console.WriteLine("##################################################");
            Console.WriteLine();
        }

        /// <summary>Returns OS</summary>
        public static OSPlatform GetOS()
        {
            OSPlatform OS = OSPlatform.Create("placeholder");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                OS = OSPlatform.Windows;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                OS = OSPlatform.Linux;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                OS = OSPlatform.OSX;

            return OS;
        }

        /// <summary>Generates certificate</summary>
        /// <param name="certname">Certificate name</param>
        /// <param name="pw">Password of certificate</param>
        /// <param name="rootcapw">Set to Root CA Cert password</param>
        /// <param name="isRootCaCert">Set to True if generating a Root CA Cert</param>
        public static void GenerateCertificate(string certname, string pw, string rootcapw, bool isRootCaCert = false)
        {
            string SELFSIGN = null;
            string OpenSSLConf = null;
            if (isRootCaCert)
            {
                SELFSIGN = "-selfsign";
                OpenSSLConf = GenerateCertsCaConf;
            }
            else
            {
                OpenSSLConf = GenerateCertsCertsConf;
            }

            openssl("ecparam -genkey -name prime256v1 -out \"{CERTNAME}.key\"".Replace("{CERTNAME}", certname));
            openssl("req -config {OPENSSLCONF} -new -SHA256 -key \"{CERTNAME}.key\" -nodes -out \"{CERTNAME}.csr\"".Replace("{CERTNAME}", certname).Replace("{OPENSSLCONF}", OpenSSLConf));
            openssl("ca -config {OPENSSLCONF} -batch {SELFSIGN} -in \"{CERTNAME}.csr\" -out \"{CERTNAME}.crt\" -days {CERTVALIDITYDAYS}".Replace("{CERTNAME}", certname).Replace("{CERTVALIDITYDAYS}", CertValidityDays).Replace("{SELFSIGN}", SELFSIGN).Replace("{OPENSSLCONF}", OpenSSLConf));

            if (rootcapw.Length > 0)
            {
                openssl("pkcs12 -export -passout pass:\"{0}\" -inkey \"{CERTNAME}.key\" -in \"{CERTNAME}.crt\" -out \"{CERTNAME}.pfx\"".Replace("{0}", pw).Replace("{CERTNAME}", certname));
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Returns a string of OpenSSL configuration
        /// </summary>
        /// <param name="vCN">CN</param>
        /// <param name="vSAN">SAN</param>
        /// <param name="vALT">ALT</param>
        /// <param name="vCACERTNAME">CACERTNAME</param>
        /// <param name="vBC">BC</param>
        /// <param name="vKU">KU</param>
        /// <param name="vCERTVALIDITYDAYS">CERTVALIDITYDAYS</param>
        /// <returns></returns>
        public static string GetOpenSslCfg(string vCN, string vSAN, string vALT, string vCACERTNAME, string vBC, string vKU, string vCERTVALIDITYDAYS)
        {
            string data = @"[ca]
default_ca = CA_default

[CA_default]
dir = .
database = $dir/{GENERATECERTSDB}
new_certs_dir = $dir/
serial = $dir/{GENERATECERTSSERIAL}
private_key = ./{CACERTNAME}.key
certificate = ./{CACERTNAME}.crt
default_days = {CERTVALIDITYDAYS}
default_md = sha256
policy = policy_match
copy_extensions = copyall
unique_subject	= no

[policy_match]
countryName = optional
stateOrProvinceName = optional
localityName = optional
organizationName = optional
organizationalUnitName = optional
commonName = supplied
emailAddress = optional

[req]
prompt = no
distinguished_name = req_distinguished_name
req_extensions = v3_data
x509_extensions	= v3_data

[req_distinguished_name]
OU = Created by GenerateCerts
O = Created by GenerateCerts
{CN}

[v3_data]
{SAN}
basicConstraints = {BC}
keyUsage = {KU}
subjectKeyIdentifier = hash

[alt_names]
{ALT}
";

            data = data.Replace("{GENERATECERTSDB}", GenerateCertsDb);
            data = data.Replace("{GENERATECERTSSERIAL}", GenerateCertsSerial);
            data = data.Replace("{CN}", vCN);
            data = data.Replace("{SAN}", vSAN);
            data = data.Replace("{ALT}", vALT);
            data = data.Replace("{CACERTNAME}", vCACERTNAME);
            data = data.Replace("{BC}", vBC);
            data = data.Replace("{KU}", vKU);
            data = data.Replace("{CERTVALIDITYDAYS}", vCERTVALIDITYDAYS);

            return data;
        }
    }
}