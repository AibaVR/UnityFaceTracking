using uOSC;
using Thread = uOSC.DotNet.Thread;
using Udp = uOSC.DotNet.Udp;

namespace Aiba.FaceTracking
{
    public class OscServer
    {
        private int _port;
        private bool _isStarted;

#if NETFX_CORE
    Udp udp_ = new Uwp.Udp();
    Thread thread_ = new Uwp.Thread();
#else
        Udp _udp = new Udp();
        Thread _thread = new Thread();
#endif
        Parser _parser = new Parser();

        public readonly DataReceiveEvent OnDataReceived = new DataReceiveEvent();

#if UNITY_EDITOR
        public readonly DataReceiveEvent OnDataReceivedEditor = new DataReceiveEvent();
#endif

        public OscServer()
        {
        }

        public void StartServer()
        {
            if (_isStarted) return;

            _udp.StartServer(_port);
            _thread.Start(UpdateMessage);

            _isStarted = true;
        }

        public void StopServer()
        {
            if (!_isStarted) return;

            _thread.Stop();
            _udp.Stop();

            _isStarted = false;
        }

        public void UpdateReceive()
        {
            while (_parser.messageCount > 0)
            {
                var message = _parser.Dequeue();
                OnDataReceived.Invoke(message);
#if UNITY_EDITOR
                OnDataReceivedEditor.Invoke(message);
#endif
            }
        }

        public void UpdatePort(int port)
        {
            if (_port == port) return;
            
            StopServer();
            _port = port;
            StartServer();
        }

        public void UpdateMessage()
        {
            while (_udp.messageCount > 0)
            {
                var buf = _udp.Receive();
                var pos = 0;
                _parser.Parse(buf, ref pos, buf.Length);
            }
        }
    }
}