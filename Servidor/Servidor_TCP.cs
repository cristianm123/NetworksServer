using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Threading;
using System.Net.Sockets;
using System.IO;
using System.Net;

namespace Servidor
{
    class Servidor_TCP
    {
        /*        
            TcpListener--------> Espera la conexion del Cliente.        
            TcpClient----------> Proporciona la Conexion entre el Servidor y el Cliente.        
            NetworkStream------> Se encarga de enviar mensajes a traves de los sockets.        
        */

        private TcpListener server;
        private TcpClient client = new TcpClient();
        private IPEndPoint ipendpoint = new IPEndPoint(IPAddress.Any, 8000);
        private List<Connection> list = new List<Connection>();

        Connection con;


        private struct Connection
        {
            public NetworkStream stream;
            public StreamWriter streamw;
            public StreamReader streamr;
            public string nick;
        }

        public Servidor_TCP()
        {

        }

        public void Inicio()
        {

            Console.WriteLine("Servidor OK!");
            server = new TcpListener(ipendpoint);
            server.Start();

            while (true)
            {
                client = server.AcceptTcpClient();

                con = new Connection();
                con.stream = client.GetStream();
                con.streamr = new StreamReader(con.stream);
                con.streamw = new StreamWriter(con.stream);

                con.nick = con.streamr.ReadLine();

                list.Add(con);
                Console.WriteLine(con.nick + " se Ha conectado.");
                if(list.Count%2 == 0)
                {
                    list[list.Count - 2].streamw.WriteLine("Begin");
                    list[list.Count - 1].streamw.WriteLine("Begin");
                }


                Thread t = new Thread(Escuchar_conexion);

                t.Start(list.Count);
            }


        }

        void Escuchar_conexion(object o)
        {
            int i = Convert.ToInt32(o);
            Connection hcon = con;
            
            do
            {
                
                try
                {

                    if (i % 2 == 0 || list.Count != i)
                    {
                        string tmp = hcon.streamr.ReadLine();
                        Console.WriteLine(hcon.nick + ": " + tmp);
                        Connection c;
                        if (i % 2 == 0)
                        {
                            c = list[i - 2];
                        }
                        else
                        {
                            c = list[i];
                        }
                        try
                        {
                            c.streamw.WriteLine(tmp);
                            c.streamw.Flush();
                        }
                        catch
                        {
                        } 
                    }
                    
                }
                catch
                {
                    list.Remove(hcon);
                    Console.WriteLine(con.nick + " se Ha desconectado." + i);
                    break;
                }
            } while (true);
        }

    }
}
