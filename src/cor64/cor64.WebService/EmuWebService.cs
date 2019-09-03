using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net;
using cor64.WebService.Debugging;

namespace cor64.WebService
{
    public class EmuWebService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private WebSocketServer m_Server;


        public EmuWebService()
        {
            m_Server = new WebSocketServer(IPAddress.Loopback, 6464);
            m_Server.ClientConnected += ClientConnectedHandler;
            m_Server.MessageRecieved += MessageRecievedHandler;
            m_Server.ClientDisconnected += ClientDisconnectedHandler;
        }

        private void ClientDisconnectedHandler(object sender, EventArgs e)
        {
            Log.Info("A client has disconnected from the remote debugger");
        }

        private void MessageRecievedHandler(object sender, string e)
        {
            var json = JsonConvert.DeserializeObject<ReplyAction>(e);
            Log.Debug(json.Type);

            var action = new UpdateDisassemblyAction();
            action.Disassembly = new Disassembly();
            action.Disassembly.Lines = new string[] { "disasm 1", "disasm 2" };
            
            m_Server.SendMessage(JsonConvert.SerializeObject(action));
        }

        private void ClientConnectedHandler(object sender, EventArgs e)
        {
            Log.Info("A client connected to the remote debugger");
        }

        public void Start()
        {
            m_Server.Open();
        }
    }
}
