using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SysLogMonitor {
    public partial class Form1 : Form {
        SysLogd s = new SysLogd();

        List<SysLogd.MessageStruct> messages = new List<SysLogd.MessageStruct>();

        public Form1() {
            InitializeComponent();
            msgrecv = new SysLogd.MessageReceived(onMessageReceived2);
        }

        private void Form1_Load(object sender, EventArgs e) {
            Config.registerAppPath();
            Config.getKeyWrite();

            Show();

            fNormal = Font;
            fB = new Font(this.Font, FontStyle.Bold);
            fBI = new Font(this.Font, FontStyle.Bold | FontStyle.Italic);
            fI = new Font(this.Font, FontStyle.Italic);

            Application.DoEvents();
            s.StartListening(onMessageReceived);

            Application.DoEvents();
            s.SendMessage(new SysLogd.MessageStruct(SysLogd.FacilityEnum.local0, SysLogd.SeverityEnum.Info,
                "HI", "LogMonitor started"), "127.0.0.1");

            Config.readFormPos(this);
            updatetitle();
            listView1.Focus();
            listView1.Select();
        }

        void updatetitle() {
            this.Text = Application.ProductName + " " + Application.ProductVersion  + " (" + Environment.UserDomainName+"\\"+Environment.UserName + " on "+Environment.MachineName+")" + (TopMost ? "  *** CTRL-T to toggle TopMost ***" : "");
        }

        void doautosize() {
            listView1.Columns[listView1.Columns.Count - 1].Width =
                listView1.Width - 520;
            
            Height = 300;
            var s = Screen.FromRectangle(Bounds);
            Width = s.WorkingArea.Width;
            Top = s.WorkingArea.Bottom - 290;
            Left = s.WorkingArea.Left + 5;
        }

        private SysLogd.MessageReceived msgrecv;
        public void onMessageReceived(SysLogd.MessageStruct Message) {
            this.Invoke(msgrecv, Message);
        }
        public void onMessageReceived2(SysLogd.MessageStruct Message) {
            messages.Insert(0,Message);
            listView1.VirtualListSize = messages.Count;
            listView1.RedrawItems(0, messages.Count - 1, true);
        }

        Font fNormal,fB,fBI,fI;

        private void listView1_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e) {
            if (e.ItemIndex == -1) return;
            if (e.Item == null) e.Item = new ListViewItem();
            
            e.Item.SubItems.Clear();
            SysLogd.MessageStruct m = messages[e.ItemIndex];
            e.Item.Selected = m.selected;
            e.Item.Text=m.Pri.Facility.ToString();
            e.Item.SubItems.Add(m.Pri.Severity.ToString());
            e.Item.SubItems.Add(m.TimeStamp.ToString());
            e.Item.SubItems.Add(m.Hostname.ToString());
            e.Item.SubItems.Add(m.AppName.ToString());
            e.Item.SubItems.Add(m.ProcID.ToString());
            e.Item.SubItems.Add(m.MsgID.ToString());
            e.Item.SubItems.Add(m.Message.ToString());
            Font f = fNormal; Color fore = Color.Black, back = Color.Transparent;
            switch (m.Pri.Severity) {
                case SysLogd.SeverityEnum.Debug:
                    //f = fI;
                    fore = Color.Green;
                    break;
                case SysLogd.SeverityEnum.Info:
                    //f = fB;
                    fore = Color.Black;
                    break;
                case SysLogd.SeverityEnum.Notice:
                    fore = Color.Blue;
                    break;
                case SysLogd.SeverityEnum.Warning:
                    f = fB;
                    fore = Color.DarkOrange;
                    break;
                case SysLogd.SeverityEnum.Error:
                    f = fB;
                    fore = Color.Red;
                    break;
                case SysLogd.SeverityEnum.Critical:
                    f = fB;
                    back = Color.Orange;
                    fore = Color.Black;
                    break;
                case SysLogd.SeverityEnum.Alert:
                    f = fBI;
                    back = Color.Black;
                    fore = Color.Orange;
                    break;
                case SysLogd.SeverityEnum.Emergency:
                    f = fBI;
                    back = Color.Firebrick;
                    fore = Color.White;
                    break;
            }
            e.Item.Font = f;
            e.Item.BackColor = back;
            e.Item.ForeColor = fore;
        }

        private void listView1_KeyUp(object sender, KeyEventArgs e) {
            if (e.Control && e.KeyCode == Keys.C) {
                copySelection();
            } else if (e.KeyCode == Keys.Delete) {
                deleteSelection();
            } else if (e.Control && e.KeyCode == Keys.A) {
                selectAll();
            } else if (e.Control && e.KeyCode == Keys.T) {
                TopMost = !TopMost;
                updatetitle();
            } else if (e.Control && e.KeyCode == Keys.D) {
                doautosize();
            } else if (e.Control && e.KeyCode == Keys.Q) {
                this.Close();
            }else if (e.KeyCode==Keys.Enter){
                for (var i = 0; i < messages.Count; i++) {
                    if (messages[i].selected) {
                        var f = new SysLogMonitor.DetailForm(messages[i]);
                        f.Show();
                    }
                }
                
            }
        }

        void selectAll() {
            SysLogd.MessageStruct m;
            for (var i = 0; i < messages.Count; i++) {
                messages[i].selected = true;
                listView1.SelectedIndices.Add(i);
            }
            listView1.RedrawItems(0, messages.Count - 1, false);
        }

        void deleteSelection() {
            for (var i = messages.Count - 1; i >= 0; i--) {
                if (messages[i].selected) messages.RemoveAt(i);
            }
            listView1.VirtualListSize = messages.Count;
            if (messages.Count>0)
            listView1.RedrawItems(0, messages.Count -1, true);
        }

        void copySelection() {
            var s = new StringBuilder();
            SysLogd.MessageStruct m;
            for (var i = 0; i < messages.Count; i++) {
                m = messages[i];
                if (m.selected == false) continue;
                s.Append(m.Pri.Facility.ToString()); s.Append('\t');
                s.Append(m.Pri.Severity.ToString()); s.Append('\t');
                s.Append(m.TimeStamp.ToString()); s.Append('\t');
                s.Append(m.Hostname.ToString()); s.Append('\t');
                s.Append(m.AppName.ToString()); s.Append('\t');
                s.Append(m.ProcID.ToString()); s.Append('\t');
                s.Append(m.MsgID.ToString()); s.Append('\t');
                s.Append(m.Message.ToString()); s.Append('\r'); s.Append('\n');

            }
            Clipboard.Clear();
            Clipboard.SetText(""+s.ToString());
        }

        private void listView1_VirtualItemsSelectionRangeChanged(object sender, ListViewVirtualItemsSelectionRangeChangedEventArgs e) {
            for (int i = e.StartIndex; i <= e.EndIndex; i++) messages[i].selected = e.IsSelected;
        }

        private void listView1_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e) {
            messages[e.ItemIndex].selected = e.IsSelected;
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e) {

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e) {
            Config.saveFormPos(this);
        }


    }
}
