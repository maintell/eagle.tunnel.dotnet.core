using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using System.IO;

namespace eagle.tunnel.dotnet.core
{
    public class Server
    {
        public Server()
        {
            ;
        }

        public void Start(string ip, int port, int backlog)
        {
            TcpListener server;
            try
            {
                IPAddress ipa = IPAddress.Parse(ip);
                server = new TcpListener(ipa, port);
                server.Start(backlog);
                Console.WriteLine("server started: " + ip + ":" + port);
                // serverCertificate = X509Certificate.CreateFromCertFile(cert);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }

            while(true)
            {
                TcpClient client = server.AcceptTcpClient();
                Console.WriteLine("new client connected");
                Thread handleClientThread = new Thread(HandleClient);
                handleClientThread.IsBackground = true;
                handleClientThread.Start(client);
            }
        }

        private void HandleClient(object clientObj)
        {
            TcpClient socket2Client = clientObj as TcpClient;
            NetworkStream stream2client = socket2Client.GetStream();
            // SslStream sslStream2Client = new SslStream(
            //     stream2client, false);
            // try
            // {
            //     sslStream2Client.AuthenticateAsServer(
            //         serverCertificate,
            //         false,
            //         SslProtocols.Tls,
            //         true
            //     );
            //     Console.WriteLine("SSL connected");

            //     sslStream2Client.ReadTimeout = 5000;
            //     sslStream2Client.ReadTimeout = 5000;
            //     HandleSocket2Client(sslStream2Client);
            // }
            // catch (Exception e)
            // {
            //     Console.WriteLine(e.Message);
            //     sslStream2Client.Close();
            //     return;
            // }
            HandleSocket2Client(stream2client);
        }

        private void HandleSocket2Client(NetworkStream stream2client)
        {
            try
            {
                Pipe pipe0 = new Pipe(stream2client, null);
                pipe0.EncryptFrom = true;
                Pipe pipe1 = new Pipe(null, stream2client);
                pipe1.EncryptTo = true;

                byte[] buffer = pipe0.Read();
                string request = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
                Console.WriteLine("Request: " + request);
                string host = GetHost(request);
                int port = GetPort(request);
                if(host == "")
                {
                    return ;
                }
                IPAddress[] ipas = Dns.GetHostAddresses(host);
                string ip = ipas[0].ToString();
                Console.WriteLine("connect to " + host + ":" + port);
                
                TcpClient client2Server = new TcpClient(ip, port);
                NetworkStream stream2server = client2Server.GetStream();

                pipe0.To = stream2server;
                pipe1.From = stream2server;

                if(port == 443)
                {
                    string re443 = "HTTP/1.1 OK\r\n\r\n";
                    buffer = Encoding.UTF8.GetBytes(re443);
                    pipe1.Write(buffer);
                }
                else
                {
                    pipe0.Write(buffer);
                }

                pipe0.Flow();
                pipe1.Flow();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private string GetURI(string request)
        {
            StringReader reader = new StringReader(request);
            string line = reader.ReadLine();
            int ind0 = line.IndexOf(' ');
            int ind1 = line.LastIndexOf(' ');
            if(ind0 == ind1)
            {
                ind1 = line.Length;
            }
            string uri = request.Substring(ind0 + 1, ind1 - ind0 -1);
            return uri;
        }

        private bool IsNum(char c)
        {
            return '0' <= c && c <= '9';
        }

        private string GetHost(string request)
        {
            string uristr = GetURI(request);
            string host;
            if(
                uristr.Contains(":") &&
                IsNum(uristr[uristr.IndexOf(":") + 1]))
            {
                int ind = uristr.LastIndexOf(":");
                host = uristr.Substring(0, ind);
            }
            else
            {
                Uri uri = new Uri(uristr);
                host = uri.Host;
            }
            return host;
        }

        private int GetPort(string request)
        {
            string uristr = GetURI(request);
            int port;
            if(
                uristr.Contains(":") &&
                IsNum(uristr[uristr.IndexOf(":") + 1]))
            {
                int ind = uristr.IndexOf(":");
                string _port = uristr.Substring(ind + 1);
                port = int.Parse(_port);
            }
            else
            {
                Uri uri = new Uri(uristr);
                port = uri.Port;
            }
            return port;
        }
    }
}