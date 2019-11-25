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
        private List<Thread> hilos = new List<Thread>();
        private Dictionary<string, string> matches = new Dictionary<string, string>();
        private int cont = 0;
        Connection con;


        private struct Connection
        {
            public NetworkStream stream;
            public StreamWriter streamw;
            public StreamReader streamr;
            public string nick;
            public string msj;
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
                con.msj = "";

                con.nick = con.streamr.ReadLine();

                list.Add(con);
                Console.WriteLine(con.nick + " se Ha conectado: " + list.Count);
                if(matches.ContainsKey(con.nick) && matches[con.nick].Equals(list[list.Count-2].nick))
                {
                    list[list.Count - 2].streamw.WriteLine("givemelove");
                    list[list.Count - 2].streamw.Flush();
                    con.streamw.WriteLine("takeyourlove");
                    con.streamw.Flush();
                    string m = list[list.Count-2].streamr.ReadLine();
                    Console.WriteLine(m);
                    con.streamw.WriteLine(m);
                    con.streamw.Flush();
                    list[list.Count - 2].streamw.WriteLine("Continue");
                    list[list.Count - 2].streamw.Flush();
                    con.streamw.WriteLine("Continue");
                    con.streamw.Flush();
                    list[list.Count - 2].streamw.WriteLine("keep");
                    list[list.Count - 2].streamw.Flush();
                    con.streamw.WriteLine("keep");
                    con.streamw.Flush();
                    matches.Remove(con.nick);
                }
                else if (list.Count % 2 == 0)
                {
                    if(matches.ContainsValue(list[list.Count - 2].nick))
                    {
                        list[list.Count - 2].streamw.WriteLine("Restart");
                        list[list.Count - 2].streamw.Flush();
                    }
                    list[list.Count - 2].streamw.WriteLine("Begin");
                    list[list.Count - 1].streamw.WriteLine("Begin");
                }


                Thread t = new Thread(Escuchar_conexion);
                hilos.Add(t);
                t.Start(list.Count);
            }


        }

        void Escuchar_conexion(object o)
        {
            int i = Convert.ToInt32(o);

            Connection hcon = new Connection();
            hcon.nick = con.nick;
            hcon.stream = con.stream;
            hcon.streamr = con.streamr;
            hcon.streamw = con.streamw;
            hcon.msj = "";
            Console.WriteLine("Escuchando conexion de {0}: {1}", hcon.nick, i);
            Connection c = new Connection();
            do
            {
                try
                {
                    if (i % 2 == 0 || list.Count != i)
                    {
                        string tmp = hcon.streamr.ReadLine();
                        Console.WriteLine(hcon.nick + ": " + tmp);
                        
                        if (i % 2 == 0)
                        {
                            c = list[i - 2];
                        }
                        else
                        {
                            c = list[i];
                        }


                        c.streamw.WriteLine(tmp);
                        c.streamw.Flush();
                      
                    }
                    
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.StackTrace);
                    
                    
                    Console.WriteLine(hcon.nick + " se Ha desconectado." + i);
                    if (cont % 2 == 0)
                    {
                        waiting(c, hcon.nick);
                        cont++;
                        matches.Add(hcon.nick, c.nick);
                    }
                    else
                    {
                        cont++;
                    }
                    
                    break;
                }
            } while (true);
            try
            {
                c.streamw.WriteLine("Wait");
                c.streamw.Flush();
            }
            catch
            {
                Console.WriteLine("c");
            }
            
            


        }

        private void waiting(Connection c, string nick)
        {
            Connection p = new Connection();
            p.nick = c.nick;
            p.stream = c.stream;
            p.streamr = c.streamr;
            p.streamw = c.streamw;
            list.Add(p);
            Console.WriteLine("se añadio al final: " + c.nick + ", se elimino a: " + nick);
            con = c;
            Thread t = new Thread(Escuchar_conexion);
            hilos.Add(t);
            t.Start(list.Count);
        }


    }
}
