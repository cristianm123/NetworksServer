using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Servidor
{
    class Server
    {
        private Servidor_TCP tcp;
        private Servidor_UDP udp;

        public Server()
        {
            tcp = new Servidor_TCP();
            Thread t1 = new Thread(tcp.Inicio);
            udp = new Servidor_UDP();
            Thread t2 = new Thread(udp.Inicio);
            t1.Start();
            t2.Start();

        }
    }
}
