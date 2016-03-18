using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Collections;

namespace SysLogMonitor {
    public class SysLogd {
        //DETERMINES WHAT PORT TO RUN ON (DEFAULT IS 514)
        private int mPort = 514;
        public int Port {
            get {
                return mPort;
            }
            set {
                mPort = value;
            }
        }

        //USED TO THROW AN EVENT WHEN DATA IS RECEIVED
        public delegate void MessageReceived(MessageStruct Message);
        private MessageReceived mCallback = null;

        //USED TO RUN THE BACKGROUND THREAD READING FOR SYSLOG PACKETS
        private System.Threading.Thread mThread;
        private bool mRunning = false;

        public void StartListening(MessageReceived CallBack) {
            //STORE THE CALLBACK
            mCallback = CallBack;

            //CREATE A NEW THREAD USING OUR THREADSTART METHOD MAKE SURE ITS A BACKGROUND THREAD SO WHEN WE EXIT IT DOES TO
            mThread = new System.Threading.Thread(ThreadStart);
            mRunning = true;
            mThread.IsBackground = true;

            //START LISTENING FOR SYSLOG PACKETS
            mThread.Start();
        }
        public void StopListening() {
            //FLAG OUR RUNNING VARIABLE TO FALSE SO WE WILL STOP LISTENING
            mRunning = false;
        }
        public void SendMessage(MessageStruct Message, string RemoteHost) {
            UdpClient sendSocket = new UdpClient(RemoteHost, this.Port);
            byte[] output = System.Text.ASCIIEncoding.ASCII.GetBytes(
                string.Format("<{0}>{1} {2} {3} {4} {5} {6} {7} {8}", 
                ((int)Message.Pri.Facility * 8) + (int)Message.Pri.Severity, 
                1, //1
                Message.TimeStamp.ToString("yyyy-MM-ddThh:mm:ssZ"),  //2
                System.Environment.MachineName, //3
                Message.AppName,   //4
                Message.ProcID,    //5
                Message.MsgID,     //6
                Message.GetStructuredDataString(),     //7
                Message.Message)); //8

            //MAKE SURE THE FINAL BUFFER IS LESS THEN 1024 BYTES AND IF SO THEN SEND THE DATA
            if (output.Length < 1024) {
                sendSocket.Send(output, output.Length);
                sendSocket.Close();
            } else {
                throw new InsufficientMemoryException("The data in which you are trying to send does not comply to syslog standards.\nThe total message size must be less then 1024 bytes.");
            }
        }

        private void ThreadStart() {
            Socket ListenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint LocalEndPoint = new IPEndPoint(IPAddress.Any, 514);
            byte[] buffer = new byte[1024];

            //BIND TO THE SOCKET SO WE CAN START READING FOR DATA
            ListenSocket.Bind(LocalEndPoint);
            while (mRunning) {
                try {
                    //READ THE DATA AND IF THERE ISNT ANY DATA THEN WAIT UNTIL THERE IS
                    EndPoint remoteEP = LocalEndPoint;
                    int BytesRead = ListenSocket.ReceiveFrom(buffer, 0, 1024, SocketFlags.None, ref remoteEP);
                    string msg = System.Text.ASCIIEncoding.ASCII.GetString(buffer, 0, BytesRead);

                    //PARSE THE MESSAGE AND RAISE THE CALL BACK
                    MessageStruct tmpReturn = new MessageStruct(msg, remoteEP);
                    mCallback(tmpReturn);
                } catch (Exception e) {
                    Console.Write(e.ToString());
                }
            }

            //CLOSE THE SOCKET SINCE WE ARE DONE WITH IT
            ListenSocket.Close();
        }

        public enum FacilityEnum : int {
            Kernel = 0,
            User = 1,
            Mail = 2,
            System = 3,
            Security = 4,
            Internally = 5,
            Printer = 6,
            News = 7,
            UUCP = 8,
            cron = 9,
            Security2 = 10,
            Ftp = 11,
            Ntp = 12,
            Audit = 13,
            Alert = 14,
            Clock2 = 15,
            local0 = 16,
            local1 = 17,
            local2 = 18,
            local3 = 19,
            local4 = 20,
            local5 = 21,
            local6 = 22,
            local7 = 23,
        }
        public enum SeverityEnum : int {
            Emergency = 0,
            Alert = 1,
            Critical = 2,
            Error = 3,
            Warning = 4,
            Notice = 5,
            Info = 6,
            Debug = 7,
        }

        public struct PriStruct {
            public FacilityEnum Facility;
            public SeverityEnum Severity;
            public PriStruct(string strPri) {
                int intPri = Convert.ToInt32(strPri);
                int intFacility = intPri >> 3;
                int intSeverity = intPri & 0x7;
                this.Facility = (FacilityEnum)Enum.Parse(typeof(FacilityEnum),
                   intFacility.ToString());
                this.Severity = (SeverityEnum)Enum.Parse(typeof(SeverityEnum),
                   intSeverity.ToString());
            }
            public PriStruct(FacilityEnum f, SeverityEnum s) {
                Facility = f; Severity = s;
            }
            public override string ToString() {
                //EXPORT VALUES TO A VALID PRI STRUCTURE
                return string.Format("{0}.{1}", this.Facility, this.Severity);
            }
        }
        public class MessageStruct {
            public PriStruct Pri { get; set; }
            public DateTime _timeStamp;
            public DateTime TimeStamp { get { return _timeStamp; } set { _timeStamp = value; } }
            public EndPoint Source { get; set; }
            public string Message { get; set; }
            public string Hostname { get; set; }
            public string AppName { get; set; }
            public string ProcID { get; set; }
            public string MsgID { get; set; }
            public string StructuredDataId { get; set; }
            public Dictionary<string, string> StructuredData { get; set; }
            public string raw;
            public bool selected;

            private static System.Diagnostics.Process curProc = System.Diagnostics.Process.GetCurrentProcess();
            private static string curProcId = curProc.Id.ToString();
            private static string curProcName = curProc.MainModule.ModuleName.Length > 47 ? curProc.MainModule.ModuleName.Substring(0, 47) : curProc.MainModule.ModuleName;


            public MessageStruct(FacilityEnum f, SeverityEnum s, string MsgId, string Message) {
                this.Pri = new PriStruct(f, s);
                this.TimeStamp = DateTime.Now;
                this.Source = null;
                this.Message = Message;
                this.Hostname = Environment.MachineName;

                this.AppName = curProcName;
                this.ProcID = curProcId;
                this.MsgID = MsgId;
                this.StructuredDataId = "-";
                this.StructuredData = new Dictionary<string, string>();
            }

            public MessageStruct(FacilityEnum f,Exception ex)
                : this(f, SeverityEnum.Error, ex.GetType().Name, ex.Message) {
                this.StructuredDataId = ex.GetType().FullName;
                this.StructuredData = new Dictionary<string, string>();
                foreach (DictionaryEntry k in ex.Data)
                    this.StructuredData.Add("Data_"+k.Key.ToString(), k.Value == null ? "NULL" : k.Value.ToString());
                this.StructuredData.Add("Source", ex.Source);
                if (ex.InnerException!=null)
                    this.StructuredData.Add("InnerException", ex.InnerException.Message);
                this.StructuredData.Add("StackTrace", ex.StackTrace);
                
            }

            public String GetStructuredDataString() {
                if (String.IsNullOrEmpty(this.StructuredDataId) || this.StructuredDataId == "-")
                    return "-";
                StringBuilder s = new StringBuilder();
                s.Append('[');
                toParamName(s, this.StructuredDataId);
                foreach (var pair in StructuredData) {
                    s.Append(' ');
                    toParamName(s, pair.Key);
                    s.Append("=\"");
                    toParamValue(s, pair.Value);
                    s.Append('"');
                }
                return s.ToString();
            }

            public static void toParamValue(StringBuilder s, string str) {
                for (int i = 0; i < str.Length; i++) {
                    char c = str[i];
                    if (c == '"') s.Append("\\\"");
                    if (c == '\\') s.Append("\\\\");
                    if (c == ']') s.Append("\\]");
                    if (c == '\r') s.Append("\\r");
                    if (c == '\n') s.Append("\\n");
                }
            }

            public static void toParamName(StringBuilder s, string str) {
                for (int i = 0; i < str.Length; i++) {
                    char c = str[i];
                    if (c > 32 && c < 127 && c != '=' && c != ' ' && c != ']' && c != '"')
                        s.Append(c);
                }
            }

            public MessageStruct(string Message, EndPoint RemoteEP) {
                this.Source = RemoteEP;
                raw = Message;
                this.StructuredData = new Dictionary<string, string>();
                try {
                    Regex mRegex = new Regex("<(?<PRI>([0-9]{1,3}))>(?<Ver>[0-9]{1,3}) " +
                        "(?<TS>[0-9A-Z:.-]{1,40}) " +
                        "(?<Host>[^ ]{1,255}) " +
                        "(?<App>[^ ]{1,48}) " +
                        "(?<PID>[^ ]{1,128}) " +
                        "(?<MID>[^ ]{1,32}) " +
                        "(?<Message>.*)", RegexOptions.Compiled);
                    Match tmpMatch = mRegex.Match(Message);
                    if (!tmpMatch.Success) {
                        throw new Exception("invalid protocol");
                    }
                    this.Pri = new PriStruct(tmpMatch.Groups["PRI"].Value);
                    this.AppName = tmpMatch.Groups["App"].Value;
                    this.ProcID = tmpMatch.Groups["PID"].Value;
                    this.MsgID = tmpMatch.Groups["MID"].Value;
                    this.Hostname = tmpMatch.Groups["Host"].Value;
                    this.Message = tmpMatch.Groups["Message"].Value;
                    parseStructuredData();

                    if (!DateTime.TryParse(tmpMatch.Groups["TS"].Value, out _timeStamp))
                        _timeStamp = DateTime.Now;
                } catch (Exception ex) {
                    this.Pri = new PriStruct(FacilityEnum.Internally, SeverityEnum.Critical);
                    this.Hostname = RemoteEP.ToString();
                    this.AppName = "SYSLOGMONITOR";
                    this.ProcID = "-";
                    this.MsgID = "ProtoERR";
                    this.Message = ex.Message + " ;; " + Message;
                    return;
                }
            }

            public void parseStructuredData() {
                char[] m = Message.ToCharArray();
                bool inStr, inBr, escaped;
                int i;
                for (i = 0; i < m.Length; i++) {
                    if (m[i] == ']' || m[i] == '-') { i++; break; }
                    if (m[i] == '[') {
                        i++;
                        StructuredDataId = parseParamName(ref m, ref i);
                        
                        for (; i < m.Length; i++) {
                            if (m[i] == ']') break;
                            i++;
                            string k = parseParamName(ref m, ref i);
                            if (m[i++] != '=') throw new Exception("invalid structuredData");
                            string v = parseParamValue(ref m, ref i);
                            StructuredData[k] = v;
                        }
                    }
                    if (m[i] == ']') break;
                }
                Message = Message.Substring(i + 1);
            }

            public string parseParamName(ref char[] m, ref int i) {
                StringBuilder pn = new StringBuilder();
                while (m[i] > 32 && m[i] < 127 && m[i] != '=' && m[i] != ' ' && m[i] != ']' && m[i] != '"' && i < m.Length)
                    pn.Append(m[i++]);
                return pn.ToString();
            }
            public string parseParamValue(ref char[] m, ref int i) {
                StringBuilder pn = new StringBuilder();
                if (m[i++] != '"') return null;
                bool escaped = false;
                while (m[i] != '"' && m[i] != ']' || escaped) {
                    if (escaped && m[i] == 'r') { pn.Append('\r'); i++; escaped = false; continue; }
                    if (escaped && m[i] == 'n') { pn.Append('\n'); i++; escaped = false; continue; }
                    if (escaped) { pn.Append(m[i++]); escaped = false; continue;  }
                    if (m[i] == '\\') { escaped = true; i++;  continue; }
                    pn.Append(m[i++]);
                }
                if (m[i] != '"') return null;
                return pn.ToString();
            }

            public override string ToString() {
                return string.Format("{0} {1} : {2} : {3} : {4}", Pri.Facility, Pri.Severity, Source.ToString().Split(':')[0], TimeStamp.ToString(), Message);
            }
        }
    }
}
