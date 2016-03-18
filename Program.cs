using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Threading;

namespace SysLogMonitor {
    static class Program {
        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static void Main() {
            bool newInst;
            Mutex m = new Mutex(true, "SysLogMonitor_singleinstance", out newInst);
            Application.EnableVisualStyles();
            if (!newInst) {
                MessageBox.Show("This program can be started only once");
                return;
            }
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
