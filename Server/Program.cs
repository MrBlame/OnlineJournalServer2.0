using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            MessageHandler messageHandler = new MessageHandler();
            messageHandler.StartServer();
            messageHandler.StartListening();
            messageHandler.WaitForClosing();
        }
    }
}
