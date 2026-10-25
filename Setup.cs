using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace NotesSetup
{
    internal static class Program
    {
        private static readonly string InstallRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Notes");
        private static readonly string StartMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "Notes");
        [STAThread]
        private static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--cleanup") { Cleanup(args); return; }
            if (args.Length > 0 && args[0] == "--uninstall") { BeginUninstall(); return; }
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); Application.Run(new InstallerForm());
        }
        private static void Install()
        {
            Directory.CreateDirectory(InstallRoot); Directory.CreateDirectory(StartMenu);
            using (Stream source = Assembly.GetExecutingAssembly().GetManifestResourceStream("Notes.exe"))
            {
                if (source == null) throw new InvalidOperationException("The Notes application could not be found inside this installer.");
                using (var target = File.Create(Path.Combine(InstallRoot, "Notes.exe"))) source.CopyTo(target);
            }
            using (Stream license = Assembly.GetExecutingAssembly().GetManifestResourceStream("Notes.LICENSE")) using (var target = File.Create(Path.Combine(InstallRoot, "LICENSE.txt"))) license.CopyTo(target);
            File.Copy(Assembly.GetExecutingAssembly().Location, Path.Combine(InstallRoot, "Uninstall Notes.exe"), true);
            Shortcut.Create(Path.Combine(StartMenu, "Notes.lnk"), Path.Combine(InstallRoot, "Notes.exe"), InstallRoot, "Notes");
            Shortcut.Create(Path.Combine(StartMenu, "Uninstall Notes.lnk"), Path.Combine(InstallRoot, "Uninstall Notes.exe"), InstallRoot, "Uninstall Notes");
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\Notes"))
            {
                key.SetValue("DisplayName", "Notes"); key.SetValue("DisplayVersion", "1.0.0"); key.SetValue("Publisher", "Notes"); key.SetValue("InstallLocation", InstallRoot);
                key.SetValue("UninstallString", "\"" + Path.Combine(InstallRoot, "Uninstall Notes.exe") + "\" --uninstall"); key.SetValue("NoModify", 1, RegistryValueKind.DWord); key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            }
        }
        private static void BeginUninstall()
        {
            if (Process.GetProcessesByName("Notes").Length > 0)
            { MessageBox.Show("Close Notes before uninstalling, then try again.", "Uninstall Notes", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            if (MessageBox.Show("Remove Notes from this PC? Your saved notes and settings will remain in your local app data folder.", "Uninstall Notes", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            string helper = Path.Combine(Path.GetTempPath(), "NotesCleanup-" + Guid.NewGuid().ToString("N") + ".exe");
            File.Copy(Assembly.GetExecutingAssembly().Location, helper);
            Process.Start(new ProcessStartInfo { FileName = helper, Arguments = "--cleanup \"" + InstallRoot + "\" " + Process.GetCurrentProcess().Id, UseShellExecute = false, CreateNoWindow = true, WindowStyle=ProcessWindowStyle.Hidden });
        }
        private static void Cleanup(string[] args)
        {
            int parent = Int32.Parse(args[2]);
            try { Process.GetProcessById(parent).WaitForExit(15000); } catch { }
            try { if (Directory.Exists(StartMenu)) Directory.Delete(StartMenu, true); } catch { }
            try { using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall", true)) if (key != null) key.DeleteSubKeyTree("Notes", false); } catch { }
            try { if (Directory.Exists(args[1])) Directory.Delete(args[1], true); } catch { }
            try { MoveFileEx(Assembly.GetExecutingAssembly().Location, null, 4); } catch { }
        }
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool MoveFileEx(string existing, string destination, int flags);

        private sealed class InstallerForm : Form
        {
            private readonly Label status;
            public InstallerForm()
            {
                Text = "Install Notes"; Width = 490; Height = 330; MinimumSize = new System.Drawing.Size(490,330); MaximumSize = new System.Drawing.Size(490,330);
                StartPosition = FormStartPosition.CenterScreen; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = true;
                BackColor = System.Drawing.Color.FromArgb(246,243,236); Font = new System.Drawing.Font("Segoe UI", 10F);
                var title = new Label { Text="Notes", Font=new System.Drawing.Font("Segoe UI Semibold",22F), ForeColor=System.Drawing.Color.FromArgb(43,40,35), Left=34,Top=28,AutoSize=true };
                var desc = new Label { Text="A quiet place for your notes.", Font=new System.Drawing.Font("Segoe UI",11F), ForeColor=System.Drawing.Color.FromArgb(99,94,86), Left=36,Top=78,AutoSize=true };
                var path = new Label { Text="Install for this Windows account", Font=new System.Drawing.Font("Segoe UI Semibold",10F), ForeColor=System.Drawing.Color.FromArgb(55,52,47), Left=36,Top=132,AutoSize=true };
                var directory = new Label { Text=InstallRoot, Font=new System.Drawing.Font("Segoe UI",9F), ForeColor=System.Drawing.Color.FromArgb(99,94,86), Left=36,Top=158,AutoSize=false,Width=400,Height=40 };
                status = new Label { Text="Adds Notes to your Start menu. Startup stays off.", Font=new System.Drawing.Font("Segoe UI",9F), ForeColor=System.Drawing.Color.FromArgb(99,94,86), Left=36,Top=207,AutoSize=false,Width=410,Height=26 };
                var install = new Button { Text="Install", Left=270,Top=244,Width=96,Height=38,DialogResult=DialogResult.None,BackColor=System.Drawing.Color.FromArgb(45,43,39),ForeColor=System.Drawing.Color.White,FlatStyle=FlatStyle.Flat };
                install.FlatAppearance.BorderSize=0; install.Click += (s,e)=> { try { Program.Install(); status.Text="Installed. Notes is ready from your Start menu."; install.Enabled=false; } catch(Exception ex) { MessageBox.Show("Notes could not be installed. "+ex.Message,"Install Notes",MessageBoxButtons.OK,MessageBoxIcon.Error); } };
                var cancel = new Button { Text="Close", Left=374,Top=244,Width=74,Height=38,DialogResult=DialogResult.Cancel,FlatStyle=FlatStyle.Flat,BackColor=System.Drawing.Color.FromArgb(237,233,225),ForeColor=System.Drawing.Color.FromArgb(50,47,42) };
                Controls.AddRange(new Control[]{title,desc,path,directory,status,install,cancel}); AcceptButton=install; CancelButton=cancel;
            }
        }
    }

    internal static class Shortcut
    {
        public static void Create(string file, string target, string workingDirectory, string description)
        {
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) throw new InvalidOperationException("Windows could not create a Start menu shortcut.");
            object shell = Activator.CreateInstance(shellType);
            object link = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { file });
            Type type = link.GetType(); type.InvokeMember("TargetPath", BindingFlags.SetProperty, null, link, new object[] { target });
            type.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, link, new object[] { workingDirectory });
            type.InvokeMember("Description", BindingFlags.SetProperty, null, link, new object[] { description });
            type.InvokeMember("Save", BindingFlags.InvokeMethod, null, link, null);
            Marshal.FinalReleaseComObject(link); Marshal.FinalReleaseComObject(shell);
        }
    }
}
