using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SysLogMonitor {
    public partial class DetailForm : Form {
        public DetailForm() {
            InitializeComponent();
        }

        private void DetailForm_Load(object sender, EventArgs e) {

        }

        public DetailForm(SysLogd.MessageStruct msg) : this() {
            TreeNode t;
            t=treeView1.Nodes.Add("Priority");
            t.Nodes.Add("Facility: " + msg.Pri.Facility.ToString());
            t.Nodes.Add("Severity: " + msg.Pri.Severity.ToString());
            t = treeView1.Nodes.Add("Header");
            t.Nodes.Add("Timestamp: " + msg.TimeStamp.ToString());
            t.Nodes.Add("Hostname: " + msg.Hostname.ToString());
            t.Nodes.Add("Application Name: " + msg.AppName.ToString());
            t.Nodes.Add("Process ID: " + msg.ProcID.ToString());
            t.Nodes.Add("Message ID: " + msg.MsgID.ToString());
            t = treeView1.Nodes.Add("Message");
            var t2 = t.Nodes.Add("Structured Data" );
            if (!String.IsNullOrEmpty(msg.StructuredDataId)) {
                var t3 = t2.Nodes.Add(msg.StructuredDataId);
                foreach (var pair in msg.StructuredData) {
                    var t4 = t3.Nodes.Add(pair.Key);

                    foreach(string line in pair.Value.Replace("\r","").Split('\n'))
                        t4.Nodes.Add(line);
                }
            }
            t.Nodes.Add("Message Body: " + msg.Message.ToString());

            treeView1.ExpandAll();
        }

    }
}
