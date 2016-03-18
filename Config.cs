using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using System.Windows.Forms;

namespace SysLogMonitor
{
    public class Config
    {


        public static RegistryKey getKeyRead() {
            return Registry.CurrentUser.OpenSubKey("Software\\Weller IT\\SysLogMonitor", false);
        }
        public static RegistryKey getKeyWrite() {
            return Registry.CurrentUser.CreateSubKey("Software\\Weller IT\\SysLogMonitor");
        }

        public static String readPreference(String name, String defaultValue) {
            using (RegistryKey key = getKeyRead()) {
                return (String)key.GetValue(name, defaultValue);
            }
        }
        public static int readPreferenceInt(String name, int defaultValue) {
            using (RegistryKey key = getKeyRead()) {
                return (int)key.GetValue(name, defaultValue);
            }
        }
        public static bool readPreferenceBool(String name, bool defaultValue) {
            using (RegistryKey key = getKeyRead()) {
                return ((int)key.GetValue(name, defaultValue ? 1 : 0) == 1);
            }
        }
        public static string[] readPreferenceStringArray(String name) {
            using (RegistryKey key = getKeyRead()) {
                return (string[])key.GetValue(name, new string[]{});
            }
        }

        public static void writePreference(String name, bool newValue) {
            using (RegistryKey key = getKeyWrite()) {
                key.SetValue(name, newValue ? 1 : 0, RegistryValueKind.DWord);
            }
        }

        public static void writePreference(String name, string newValue) {
            using (RegistryKey key = getKeyWrite()) {
                key.SetValue(name, newValue, RegistryValueKind.String);
            }
        }

        public static void writePreference(String name, int newValue) {
            using (RegistryKey key = getKeyWrite()) {
                key.SetValue(name, newValue, RegistryValueKind.DWord);
            }
        }
        public static void writePreference(String name, IEnumerable<String> newValue) {
            using (RegistryKey key = getKeyWrite()) {
                key.SetValue(name, newValue.ToArray(), RegistryValueKind.MultiString);
            }
        }

        public static bool preferenceExists(String name) {
            using (RegistryKey key = getKeyRead()) {
                return key.GetValue(name) != null;
            }
        }

        public static void readControlPos(Control frm) {
            readControlPos(frm, true, true);
        }
        public static void readControlPos(Control frm, bool readPosition, bool readSize) {
            String[] value = readPreference("Form Position " + frm.Name, "").Split(new string[] { " ", "," }, StringSplitOptions.RemoveEmptyEntries);
            if (value.Length != 4) return;
            int i;
            if (readPosition) {
                if (Int32.TryParse(value[0], out i)) frm.Left = i;
                if (Int32.TryParse(value[1], out i)) frm.Top = i;
            }
            if (readSize) {
                if (Int32.TryParse(value[2], out i)) frm.Width = i;
                if (Int32.TryParse(value[3], out i)) frm.Height = i;
            }
        }

        public static void saveControlPos(Control frm) {
            String value = String.Format("{0}, {1}, {2}, {3}", frm.Left, frm.Top, frm.Width, frm.Height);
            writePreference("Form Position " + frm.Name, value);
        }

        public static void readFormPos(Form frm) {
            readControlPos(frm, true, frm.FormBorderStyle==FormBorderStyle.Sizable||frm.FormBorderStyle==FormBorderStyle.SizableToolWindow||frm.FormBorderStyle==FormBorderStyle.None);
            switch (readPreference("Form WindowState " + frm.Name, "Normal")) {
                case "Maximized": frm.WindowState = FormWindowState.Maximized; break;
            }
        }

        public static void saveFormPos(Form frm) {
            var oldState = frm.WindowState;
            frm.WindowState = FormWindowState.Normal;
            saveControlPos(frm);
            frm.WindowState = oldState;
            writePreference("Form WindowState " + frm.Name, frm.WindowState.ToString());
        }



        public static void updateMru(string id, string content, int maxLength) {
            var mru = readMru(id);
            if (mru.Contains(content)) mru.Remove(content);
            mru.Insert(0, content);
            if (mru.Count > maxLength) mru.RemoveRange(maxLength, mru.Count - maxLength);
            writePreference("MRU " + id, mru);
        }

        public static List<String> readMru(String id) {
            List<String> mru = new List<string>(readPreferenceStringArray("MRU "+id));
            return mru;
        }

        public static string getAppPath() {
            return System.IO.Path.GetDirectoryName(Application.ExecutablePath);
        }

        public static void registerAppPath() {
            string exePath = Application.ExecutablePath;
            string exeName = System.IO.Path.GetFileName(exePath);
            using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths\" + exeName)) {
                key.SetValue(null, exePath);
            }
        }

    }
}
