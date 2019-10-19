using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            udp = new Servidor_UDP();

        }
    }
}
