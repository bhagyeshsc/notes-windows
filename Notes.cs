using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;
using System.Windows.Automation;
using System.Windows.Threading;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace NotesApp
{
    public sealed class Note
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public bool AlwaysOnTop { get; set; }
        public List<NoteLink> Links { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public Note() { Id = Guid.NewGuid().ToString("N"); Title = "Untitled"; Body = ""; Links=new List<NoteLink>(); UpdatedUtc = DateTime.UtcNow; }
    }

    public sealed class Preferences
    {
        public string Theme { get; set; }
        public double TextSize { get; set; }
        public bool DefaultTemporary { get; set; }
        public bool RestoreFloatingNotes { get; set; }
        public bool LaunchAtStartup { get; set; }
        public string CreatorName { get; set; }
        public List<string> FloatingNoteIds { get; set; }
        public Preferences() { Theme="Follow Windows"; TextSize=18; CreatorName=""; FloatingNoteIds=new List<string>(); }
    }

    internal sealed class Palette
    {
        public string Background, Surface, Raised, Text, Muted, Border, Hover, Selected, Accent, AccentText, Track;
        public static Palette For(string mode)
        {
            bool dark = mode == "Dark" || (mode == "Follow Windows" && IsWindowsDark());
            return dark ? new Palette { Background="#202020", Surface="#171717", Raised="#242424", Text="#F3F0EB", Muted="#A6A19C", Border="#41403E", Hover="#30302F", Selected="#30302F", Accent="#E7B85C", AccentText="#1E1B15", Track="#69645C" }
                        : new Palette { Background="#F4F1EA", Surface="#FBF9F5", Raised="#FFFFFF", Text="#292723", Muted="#726C63", Border="#DED9D0", Hover="#EFEBE4", Selected="#EBE6DD", Accent="#94651D", AccentText="#FFFFFF", Track="#8A8276" };
        }
        private static bool IsWindowsDark()
        {
            try { using (var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")) return Convert.ToInt32(k == null ? 1 : k.GetValue("AppsUseLightTheme", 1)) == 0; }
            catch { return false; }
        }
    }

    internal static class Store
    {
        public static string Root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Notes");
        private static string NotesFile { get { return Path.Combine(Root, "notes.json"); } }
        private static string PrefsFile { get { return Path.Combine(Root, "settings.json"); } }
        public static List<Note> Notes = new List<Note>();
        public static Preferences Prefs = new Preferences();
        public static void Load()
        {
            Directory.CreateDirectory(Root);
            Notes = Read<List<Note>>(NotesFile) ?? new List<Note>();
            Prefs = Read<Preferences>(PrefsFile) ?? new Preferences();
            if (Prefs.FloatingNoteIds == null) Prefs.FloatingNoteIds = new List<string>();
            Notes = Notes.Where(n => n != null && !String.IsNullOrWhiteSpace(n.Id)).ToList();
            foreach(var note in Notes)if(note.Links==null)note.Links=new List<NoteLink>();
        }
        private static T Read<T>(string path) where T : class
        {
            try { if (File.Exists(path)) return new System.Web.Script.Serialization.JavaScriptSerializer { MaxJsonLength = 10000000 }.Deserialize<T>(File.ReadAllText(path)); }
            catch { try { if (File.Exists(path + ".bak")) return new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<T>(File.ReadAllText(path + ".bak")); } catch { } }
            return null;
        }
        private static void Write<T>(string path, T value)
        {
            Directory.CreateDirectory(Root);
            string tmp = path + ".tmp";
            string json = new System.Web.Script.Serialization.JavaScriptSerializer { MaxJsonLength = 10000000 }.Serialize(value);
            File.WriteAllText(tmp, json, new System.Text.UTF8Encoding(false));
            if (File.Exists(path)) { try { File.Replace(tmp, path, path + ".bak", true); } catch { File.Copy(path, path + ".bak", true); File.Delete(path); File.Move(tmp, path); } }
            else File.Move(tmp, path);
        }
        public static void SaveNotes() { Write(NotesFile, Notes); }
        public static void SavePrefs() { Write(PrefsFile, Prefs); }
        public static void Touch(Note n) { n.UpdatedUtc = DateTime.UtcNow; SaveNotes(); }
        public static bool CommitEdit(Note n, bool temporary)
        {
            bool exists = Notes.Any(x => x.Id == n.Id);
            if (temporary && !exists) return false;
            if (!exists) Notes.Insert(0, n);
            if (String.IsNullOrWhiteSpace(n.Title)) n.Title = "Untitled";
            n.UpdatedUtc = DateTime.UtcNow; SaveNotes(); return true;
        }
    }

    internal static class AppStyle
    {
        public static Palette P { get { return Palette.For(Store.Prefs.Theme); } }
        public static Brush Caret { get { return P.Surface=="#171717" ? Brushes.White : B(P.Text); } }
        public static Brush B(string hex) { return (Brush)new BrushConverter().ConvertFromString(hex); }
        public static Style TextBox(bool title = false)
        {
            var s = new Style(typeof(TextBox));
            s.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent)); s.Setters.Add(new Setter(Control.ForegroundProperty, B(P.Text)));
            s.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0))); s.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
            s.Setters.Add(new Setter(Control.FontFamilyProperty, new FontFamily("Segoe UI Variable Text, Segoe UI")));
            s.Setters.Add(new Setter(System.Windows.Controls.TextBox.CaretBrushProperty, Caret));
            s.Setters.Add(new Setter(WindowChrome.IsHitTestVisibleInChromeProperty,true));
            s.Setters.Add(new Setter(System.Windows.Controls.TextBox.AcceptsReturnProperty, !title));
            s.Setters.Add(new Setter(System.Windows.Controls.TextBox.TextWrappingProperty, title?TextWrapping.NoWrap:TextWrapping.Wrap)); s.Setters.Add(new Setter(FrameworkElement.FocusVisualStyleProperty, null));
            s.Setters.Add(new Setter(ScrollViewer.VerticalScrollBarVisibilityProperty, title ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto));
            var focus = new Trigger { Property=UIElement.IsKeyboardFocusedProperty, Value=true };
            focus.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0,0,0,1)));
            focus.Setters.Add(new Setter(Control.BorderBrushProperty, B(P.Accent)));
            s.Triggers.Add(focus);
            return s;
        }
        public static Button Button(string label, Action action, bool icon = false)
        {
            var b = new Button { Content = label=="···" ? (object)Icons.Create("more") : label=="×" ? (object)Icons.Create("close") : label, ToolTip = label == "···" ? null : label, MinWidth = icon ? 38 : 0, Height = icon ? 38 : 36, Padding = icon ? new Thickness(5) : new Thickness(12, 6, 12, 6),
                Background = Brushes.Transparent, Foreground = B(P.Muted), BorderThickness = new Thickness(0), Cursor = Cursors.Hand, FontSize = icon ? 19 : 13, FontFamily = new FontFamily("Segoe UI") };
            b.Click += (s,e) => action();
            WindowChrome.SetIsHitTestVisibleInChrome(b,true);DecorateButton(b);
            b.HorizontalContentAlignment=HorizontalAlignment.Center;b.Focusable = true; AutomationProperties.SetName(b, label == "···" ? "More options" : label); return b;
        }
        public static void DecorateButton(Button b)
        {
            var shell=new FrameworkElementFactory(typeof(Border));shell.SetValue(Border.CornerRadiusProperty,new CornerRadius(9));
            shell.SetBinding(Border.BackgroundProperty,new System.Windows.Data.Binding("Background"){RelativeSource=System.Windows.Data.RelativeSource.TemplatedParent});
            shell.SetBinding(Border.BorderBrushProperty,new System.Windows.Data.Binding("BorderBrush"){RelativeSource=System.Windows.Data.RelativeSource.TemplatedParent});shell.SetBinding(Border.BorderThicknessProperty,new System.Windows.Data.Binding("BorderThickness"){RelativeSource=System.Windows.Data.RelativeSource.TemplatedParent});
            var layers=new FrameworkElementFactory(typeof(Grid));
            var hoverSurface=new FrameworkElementFactory(typeof(Border));hoverSurface.Name="hoverSurface";hoverSurface.SetValue(Border.CornerRadiusProperty,new CornerRadius(9));hoverSurface.SetValue(Border.BackgroundProperty,B(P.Hover));hoverSurface.SetValue(UIElement.OpacityProperty,0.0);layers.AppendChild(hoverSurface);
            var presenter=new FrameworkElementFactory(typeof(ContentPresenter));presenter.SetValue(FrameworkElement.MarginProperty,b.Padding);presenter.SetValue(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Center);
            presenter.SetBinding(FrameworkElement.HorizontalAlignmentProperty,new System.Windows.Data.Binding("HorizontalContentAlignment"){RelativeSource=System.Windows.Data.RelativeSource.TemplatedParent});layers.AppendChild(presenter);shell.AppendChild(layers);
            var template=new ControlTemplate(typeof(Button)){VisualTree=shell};
            var focus=new Trigger{Property=UIElement.IsKeyboardFocusedProperty,Value=true};focus.Setters.Add(new Setter(Control.BackgroundProperty,B(P.Hover)));template.Triggers.Add(focus);
            var disabled=new Trigger{Property=UIElement.IsEnabledProperty,Value=false};disabled.Setters.Add(new Setter(UIElement.OpacityProperty,.4));template.Triggers.Add(disabled);
            b.Template=template;Motion.Wire(b);
        }
        public static bool IsInput(DependencyObject source){while(source!=null){if(source is TextBoxBase||source is ButtonBase||source is ScrollBar||source is Thumb)return true;source=VisualTreeHelper.GetParent(source);}return false;}
        public static void DragSurface(Window window,FrameworkElement surface){surface.MouseLeftButtonDown+=(s,e)=>{if(!e.Handled&&!IsInput(e.OriginalSource as DependencyObject)&&e.LeftButton==MouseButtonState.Pressed)try{window.DragMove();e.Handled=true;}catch{}};}
        public static TextBlock Text(string text, double size, bool muted = false, bool semibold = false)
        { return new TextBlock { Text=text, FontSize=size, Foreground=B(muted ? P.Muted : P.Text), FontWeight=semibold ? FontWeights.SemiBold : FontWeights.Normal, FontFamily=new FontFamily("Segoe UI Variable Text, Segoe UI"), VerticalAlignment=VerticalAlignment.Center }; }
        public static Border Card(UIElement child, double radius = 14)
        { return new Border { Child=child, Background=B(P.Surface), BorderBrush=B(P.Border), BorderThickness=new Thickness(1), CornerRadius=new CornerRadius(radius) }; }
        public static Button MenuButton(string icon,string text,Action action)
        {
            var row=new StackPanel{Orientation=Orientation.Horizontal};row.Children.Add(Icons.Create(icon));row.Children.Add(new TextBlock{Text=text,FontSize=13,Foreground=B(P.Text),Margin=new Thickness(12,0,0,0),VerticalAlignment=VerticalAlignment.Center});
            var button=new Button{Content=row,Height=42,Padding=new Thickness(12,4,12,4),HorizontalContentAlignment=HorizontalAlignment.Left,Background=Brushes.Transparent,BorderThickness=new Thickness(0),Cursor=Cursors.Hand};button.Click+=(s,e)=>action();DecorateButton(button);AutomationProperties.SetName(button,text);return button;
        }
        public static void ShowThemePicker(UIElement anchor,Action apply)
        {
            Popup popup=null;var rows=new StackPanel{Width=205,Margin=new Thickness(5)};
            foreach(string name in new[]{"Light","Dark","Follow Windows"}){string theme=name;var button=MenuButton(name=="Light"?"sun":name=="Dark"?"theme":"system",name,()=>{Store.Prefs.Theme=theme;Store.SavePrefs();popup.IsOpen=false;apply();});if(theme==Store.Prefs.Theme){button.Background=B(P.Selected);AutomationProperties.SetItemStatus(button,"Selected");var check=Icons.Create("check",true,14);check.Margin=new Thickness(12,0,0,0);((StackPanel)button.Content).Children.Add(check);}rows.Children.Add(button);}
            popup=new Popup{PlacementTarget=anchor,Placement=PlacementMode.Bottom,StaysOpen=false,AllowsTransparency=true,Child=Card(rows,12)};Motion.Menu(popup);popup.IsOpen=true;
        }
        public static UIElement PinIcon(bool active){return Icons.Create("pin",active);}
        public static void Chrome(Window w, double caption = 54)
        {
            Scrollbars.Apply(w);
            w.Icon=Branding.WindowIcon();w.ContentRendered+=(s,e)=>Motion.Arrive(w.Content as FrameworkElement);
            WindowChrome.SetWindowChrome(w, new WindowChrome { CaptionHeight=caption, ResizeBorderThickness=new Thickness(7), GlassFrameThickness=new Thickness(0), CornerRadius=new CornerRadius(12), UseAeroCaptionButtons=false });
            w.SourceInitialized+=(s,e)=>HwndSource.FromHwnd(new WindowInteropHelper(w).Handle).AddHook((IntPtr hwnd,int msg,IntPtr wp,IntPtr lp,ref bool handled)=>{
                if(msg==0x84 && w.WindowState==WindowState.Normal){long packed=lp.ToInt64();var p=w.PointFromScreen(new Point((short)(packed&65535),(short)((packed>>16)&65535)));int hit=ResizeHit(p,w.ActualWidth,w.ActualHeight);if(hit!=0){handled=true;return new IntPtr(hit);}}
                if(msg==0x20){int hit=(int)(lp.ToInt64()&65535);int cursor=hit==10||hit==11?32644:hit==12||hit==15?32645:hit==13||hit==17?32642:hit==14||hit==16?32643:0;if(cursor!=0){SetCursor(LoadCursor(IntPtr.Zero,new IntPtr(cursor)));handled=true;return new IntPtr(1);}}
                return IntPtr.Zero;
            });
        }
        internal static int ResizeHit(Point p,double width,double height){bool left=p.X<8,right=p.X>=width-8,top=p.Y<8,bottom=p.Y>=height-8;return top?(left?13:right?14:12):bottom?(left?16:right?17:15):left?10:right?11:0;}
        [DllImport("user32.dll")] private static extern IntPtr LoadCursor(IntPtr instance,IntPtr cursor);
        [DllImport("user32.dll")] private static extern IntPtr SetCursor(IntPtr cursor);
    }

    public sealed class NotesApp : Application
    {
        private Mutex mutex;
        private Forms.NotifyIcon tray;
        private MainWindow main;
        private readonly Dictionary<string, NoteWindow> windows = new Dictionary<string, NoteWindow>();
        private bool exiting;
        private string pipeName;
        [STAThread]
        public static void Main(string[] args) { var app = new NotesApp(); app.Start(args); }
        private void Start(string[] args)
        {
            string dataArg = args.FirstOrDefault(x => x.StartsWith("--data-dir=", StringComparison.OrdinalIgnoreCase));
            if (!String.IsNullOrEmpty(dataArg)) Store.Root = dataArg.Substring("--data-dir=".Length);
            string sid = Environment.UserName;
            pipeName = "NotesApp_" + Math.Abs(sid.GetHashCode()).ToString();
            bool created; mutex = new Mutex(true, "Local\\NotesApp_" + Math.Abs(sid.GetHashCode()).ToString(), out created);
            if (!created) { SendToRunning(args.Contains("--new") ? "new" : "activate"); mutex.Dispose(); return; }
            Store.Load(); ShutdownMode = ShutdownMode.OnExplicitShutdown;
            if (args.Contains("--self-test")) { RunStorageSelfTest(); mutex.Dispose(); return; }
            DispatcherUnhandledException += (s,e) => { e.Handled=true; MessageBox.Show(e.Exception.Message, "Notes", MessageBoxButton.OK, MessageBoxImage.Error); };
            SetupTray(); StartPipe();
            main = new MainWindow(this); if (!args.Contains("--tray")) { main.Show(); main.Activate(); }
            if (Store.Prefs.RestoreFloatingNotes)
                foreach (string id in Store.Prefs.FloatingNoteIds.ToList()) { var n=Store.Notes.FirstOrDefault(x=>x.Id==id); if(n!=null) OpenNote(n); }
            base.Run();
        }
        private void RunStorageSelfTest()
        {
            var results = new List<string>();
            if (Store.Notes.Count != 0) throw new InvalidOperationException("Self-test data folder must be empty before running.");
            var temporary = new Note { Title="Temporary test", Body="This should disappear." };
            if (Store.CommitEdit(temporary, true) || Store.Notes.Count != 0) throw new InvalidOperationException("Temporary note content entered the saved collection.");
            results.Add("Temporary note edits stayed out of saved notes.");
            var saved = new Note { Title="Saved test", Body="Local UTF-8: café — 日本語" };
            if (!Store.CommitEdit(saved, false)) throw new InvalidOperationException("A saved note was not committed.");
            Store.Prefs.Theme="Dark"; Store.Prefs.TextSize=22; Store.Prefs.DefaultTemporary=true; Store.SavePrefs();
            Store.Load();
            var restored=Store.Notes.FirstOrDefault(n=>n.Id==saved.Id);
            if (restored==null || restored.Body!=saved.Body || restored.Title!=saved.Title) throw new InvalidOperationException("Saved note content did not survive a reload.");
            if (Store.Prefs.Theme!="Dark" || Store.Prefs.TextSize!=22 || !Store.Prefs.DefaultTemporary) throw new InvalidOperationException("Settings did not survive a reload.");
            if (File.Exists(Path.Combine(Store.Root,"notes.json.tmp")) || File.Exists(Path.Combine(Store.Root,"settings.json.tmp"))) throw new InvalidOperationException("A temporary write file was left behind.");
            results.Add("Saved notes and theme preferences survived a reload.");
            File.WriteAllLines(Path.Combine(Store.Root,"self-test-report.txt"),results.ToArray(),new System.Text.UTF8Encoding(false));
        }
        private void SetupTray()
        {
            tray = new Forms.NotifyIcon { Text="Notes", Icon=Branding.TrayIcon(), Visible=true };
            var menu = new Forms.ContextMenuStrip();
            menu.Items.Add("Open Notes", null, (s,e)=>ShowMain()); menu.Items.Add("New note", null, (s,e)=>NewNote(Store.Prefs.DefaultTemporary));
            menu.Items.Add("Settings", null, (s,e)=>OpenSettings()); menu.Items.Add(new Forms.ToolStripSeparator()); menu.Items.Add("Exit", null, (s,e)=>ExitApp());
            tray.ContextMenuStrip=menu; tray.DoubleClick += (s,e)=>ShowMain(); tray.MouseClick += (s,e)=> { if(e.Button==Forms.MouseButtons.Left) ShowMain(); };
            UpdateTrayTheme();
        }
        private void UpdateTrayTheme(){if(tray==null)return;var menu=tray.ContextMenuStrip;menu.ShowImageMargin=false;menu.BackColor=System.Drawing.ColorTranslator.FromHtml(AppStyle.P.Surface);menu.ForeColor=System.Drawing.ColorTranslator.FromHtml(AppStyle.P.Text);if(menu.Font.Name!="Segoe UI"||menu.Font.Size!=10)menu.Font=new System.Drawing.Font("Segoe UI",10);menu.Renderer=new Forms.ToolStripProfessionalRenderer(new TrayColors());}
        private void StartPipe()
        {
            var t = new Thread(() => { while (!exiting) { try { using(var p=new NamedPipeServerStream(pipeName,PipeDirection.In,1,PipeTransmissionMode.Byte,PipeOptions.None)) { p.WaitForConnection(); using(var r=new StreamReader(p)) { string cmd=r.ReadLine(); Dispatcher.BeginInvoke(new Action(()=> { if(cmd=="new") NewNote(Store.Prefs.DefaultTemporary); else ShowMain(); })); } } } catch { Thread.Sleep(250); } } }); t.IsBackground=true; t.Start();
        }
        private void SendToRunning(string cmd)
        {
            for(int i=0;i<8;i++) { try { using(var p=new NamedPipeClientStream(".",pipeName,PipeDirection.Out)) { p.Connect(300); using(var w=new StreamWriter(p)){w.AutoFlush=true;w.WriteLine(cmd);} return; } } catch { Thread.Sleep(150); } }
        }
        public void ShowMain() { if(main==null) main=new MainWindow(this); MainWindow=main; if(!main.IsVisible) main.Show(); if(main.WindowState==WindowState.Minimized) main.WindowState=WindowState.Normal; main.Activate(); main.Focus(); main.RefreshNotes(true); }
        public void NewNote(bool temporary)
        {
            var n = new Note { Title="Untitled" };
            if(!temporary) { Store.Notes.Insert(0,n); Store.SaveNotes(); if(main!=null) main.RefreshNotes(); }
            OpenNote(n,temporary); 
        }
        public void OpenNote(Note n, bool temporary=false)
        {
            if(windows.ContainsKey(n.Id)) { windows[n.Id].Activate(); return; }
            var w=new NoteWindow(this,n,temporary || !Store.Notes.Any(x=>x.Id==n.Id));PositionNote(w);windows[n.Id]=w;
            w.Closed += (s,e)=> { windows.Remove(n.Id); if(!exiting&&Store.Prefs.FloatingNoteIds.Contains(n.Id)) { Store.Prefs.FloatingNoteIds.Remove(n.Id); Store.SavePrefs(); } };
            w.Show(); w.Activate();
        }
        private void PositionNote(Window window)
        {
            Window anchor=windows.Values.LastOrDefault(w=>w.IsVisible);bool anotherNote=anchor!=null;if(anchor==null&&main!=null&&main.IsVisible)anchor=main;if(anchor==null)return;
            var source=PresentationSource.FromVisual(anchor);if(source==null||source.CompositionTarget==null)return;var screenPoint=anchor.PointToScreen(new Point(0,0));var screen=Forms.Screen.FromPoint(new System.Drawing.Point((int)screenPoint.X,(int)screenPoint.Y));var transform=source.CompositionTarget.TransformFromDevice;
            var topLeft=transform.Transform(new Point(screen.WorkingArea.Left,screen.WorkingArea.Top));var bottomRight=transform.Transform(new Point(screen.WorkingArea.Right,screen.WorkingArea.Bottom));
            double x=anchor.Left+(anotherNote?28:anchor.ActualWidth-32),y=anchor.Top+(anotherNote?28:56);
            window.WindowStartupLocation=WindowStartupLocation.Manual;window.Left=Math.Max(topLeft.X+8,Math.Min(x,bottomRight.X-window.Width-8));window.Top=Math.Max(topLeft.Y+8,Math.Min(y,bottomRight.Y-window.Height-8));
        }
        public void OpenSettings() { new SettingsWindow(this).ShowDialog(); ApplyThemeToWindows(); }
        public void ApplyThemeToWindows() { if(main!=null)main.ApplyTheme();foreach(var w in windows.Values.ToList())w.ApplyTheme();UpdateTrayTheme(); }
        public void Remove(Note n) { Store.Notes.RemoveAll(x=>x.Id==n.Id); Store.Prefs.FloatingNoteIds.Remove(n.Id); Store.SaveNotes(); Store.SavePrefs(); if(windows.ContainsKey(n.Id)) windows[n.Id].CloseForDeletion(); if(main!=null) main.RefreshNotes(); }
        public void ExitApp()
        {
            Store.Prefs.FloatingNoteIds=windows.Keys.Where(id=>Store.Notes.Any(n=>n.Id==id)).ToList();exiting=true; foreach(var w in windows.Values.ToList()) w.CloseFromApp(); if(main!=null) { main.AllowExit=true; main.Close(); }
            if(tray!=null){tray.Visible=false;tray.Dispose();} Store.SaveNotes(); Store.SavePrefs(); Shutdown();
        }
        public void SaveAs(Note n) { Store.CommitEdit(n,false); if(main!=null) main.RefreshNotes(); }
        public void NoteEdited(Note n, bool temporary) { if(Store.CommitEdit(n,temporary) && main!=null) main.RefreshNotes(); }
        public void FloatingChanged(Note n, bool open) { if(open) {if(!Store.Prefs.FloatingNoteIds.Contains(n.Id))Store.Prefs.FloatingNoteIds.Add(n.Id);} else Store.Prefs.FloatingNoteIds.Remove(n.Id); Store.SavePrefs(); }
    }

    internal sealed class MainWindow : Window
    {
        private readonly NotesApp app; private StackPanel list; private ScrollViewer scroll; private Border root; private Popup menu; private TextBlock countLabel;
        private readonly Dictionary<string,Button> noteRows=new Dictionary<string,Button>(); private readonly Dictionary<string,TextBlock[]> rowText=new Dictionary<string,TextBlock[]>();
        public bool AllowExit;
        public MainWindow(NotesApp a)
        {
            app=a;Title="Notes";Width=600;Height=690;MinWidth=340;MinHeight=300;WindowStartupLocation=WindowStartupLocation.CenterScreen;
            WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.CanResize;AppStyle.Chrome(this);Build();RefreshNotes();
            Closing+=(s,e)=>{if(!AllowExit){e.Cancel=true;Hide();}};
            KeyDown+=(s,e)=>{if((Keyboard.Modifiers&ModifierKeys.Control)!=0&&e.Key==Key.N){app.NewNote(Store.Prefs.DefaultTemporary);e.Handled=true;}if(e.Key==Key.Escape&&menu!=null)menu.IsOpen=false;};
        }
        private void Build()
        {
            Background=AppStyle.B(AppStyle.P.Surface);
            Scrollbars.Apply(this);var layout=new Grid();layout.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});layout.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});layout.RowDefinitions.Add(new RowDefinition{Height=new GridLength(1,GridUnitType.Star)});
            var header=new DockPanel{Margin=new Thickness(8,0,4,22)};
            var close=AppStyle.Button("×",()=>Close(),true);close.ToolTip="Close notes list";DockPanel.SetDock(close,Dock.Right);header.Children.Add(close);Button more=null;more=AppStyle.Button("···",()=>ShowMenu(more),true);DockPanel.SetDock(more,Dock.Right);header.Children.Add(more);
            var heading=AppStyle.Text("Notes",24,false,true);heading.Cursor=Cursors.SizeAll;heading.MouseLeftButtonDown+=(s,e)=>{if(e.ClickCount==2)WindowState=WindowState==WindowState.Maximized?WindowState.Normal:WindowState.Maximized;else try{DragMove();}catch{}};var headingGroup=new StackPanel();headingGroup.Children.Add(heading);countLabel=AppStyle.Text("",12,true);countLabel.Margin=new Thickness(0,5,0,0);headingGroup.Children.Add(countLabel);header.Children.Add(headingGroup);layout.Children.Add(header);
            var actions=new UniformGrid{Columns=2,Margin=new Thickness(0,0,0,18)};actions.Children.Add(QuickAdd("Note",false));actions.Children.Add(QuickAdd("Temp note",true));Grid.SetRow(actions,1);layout.Children.Add(actions);
            list=new StackPanel();scroll=new ScrollViewer{Content=list,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};Grid.SetRow(scroll,2);layout.Children.Add(scroll);
            root=new Border{Child=layout,Background=AppStyle.B(AppStyle.P.Surface),BorderBrush=AppStyle.B(AppStyle.P.Border),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(14),Padding=new Thickness(22)};
            Content=root;AppStyle.DragSurface(this,root);noteRows.Clear();rowText.Clear();
        }
        public void RefreshNotes(){RefreshNotes(false);}
        private Button QuickAdd(string label,bool temporary){var content=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center};content.Children.Add(Icons.Create("plus",false,19));var text=AppStyle.Text(label,15,false,true);text.Margin=new Thickness(10,0,0,0);content.Children.Add(text);var button=new Button{Content=content,Height=60,Margin=temporary?new Thickness(6,0,0,0):new Thickness(0,0,6,0),Padding=new Thickness(14),Background=Brushes.Transparent,BorderBrush=AppStyle.B(AppStyle.P.Border),BorderThickness=new Thickness(1),HorizontalContentAlignment=HorizontalAlignment.Center,Cursor=Cursors.Hand};AppStyle.DecorateButton(button);AutomationProperties.SetName(button,temporary?"New temporary note":"New saved note");button.Click+=(s,e)=>app.NewNote(temporary);return button;}
        public void RefreshNotes(bool reorder)
        {
            if(list==null)return;double offset=scroll.VerticalOffset;
            countLabel.Text=Store.Notes.Count==0?"No saved notes yet":Store.Notes.Count==1?"1 saved note":Store.Notes.Count+" saved notes";
            bool changed=noteRows.Count!=Store.Notes.Count||Store.Notes.Any(n=>!noteRows.ContainsKey(n.Id));
            if(changed||reorder||Store.Notes.Count==0){
                list.Children.Clear();
                foreach(string id in noteRows.Keys.Where(id=>!Store.Notes.Any(n=>n.Id==id)).ToList()){noteRows.Remove(id);rowText.Remove(id);}
                foreach(var note in Store.Notes.OrderByDescending(n=>n.UpdatedUtc)){
                    Button row;if(!noteRows.TryGetValue(note.Id,out row)){
                        row=new Button{Background=Brushes.Transparent,BorderThickness=new Thickness(0),Padding=new Thickness(12,15,12,15),Margin=new Thickness(0,0,0,5),HorizontalContentAlignment=HorizontalAlignment.Stretch,Cursor=Cursors.Hand,Tag=note};
                        var content=new Grid();content.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(24)});content.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});content.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(22)});
                        var paper=Icons.Create("note",false,18);paper.VerticalAlignment=VerticalAlignment.Top;paper.Margin=new Thickness(0,2,0,0);content.Children.Add(paper);
                        var words=new StackPanel{Margin=new Thickness(12,0,12,0)};var title=AppStyle.Text("",16,false,true);title.TextTrimming=TextTrimming.CharacterEllipsis;var preview=AppStyle.Text("",13,true);preview.Margin=new Thickness(0,7,0,0);preview.TextTrimming=TextTrimming.CharacterEllipsis;words.Children.Add(title);words.Children.Add(preview);Grid.SetColumn(words,1);content.Children.Add(words);
                        var arrow=Icons.Create("open",false,16);arrow.Opacity=0;Grid.SetColumn(arrow,2);content.Children.Add(arrow);NameScope.SetNameScope(content,new NameScope());content.RegisterName("openCue",arrow);
                        row.Content=content;AppStyle.DecorateButton(row);row.Click+=(s,e)=>app.OpenNote((Note)((Button)s).Tag);noteRows[note.Id]=row;rowText[note.Id]=new[]{title,preview};
                    }
                    list.Children.Add(row);
                }
                if(Store.Notes.Count==0){
                    var empty=new StackPanel{Margin=new Thickness(16,42,16,0)};var paper=Icons.Create("note",true,32);paper.HorizontalAlignment=HorizontalAlignment.Left;paper.Margin=new Thickness(0,0,0,20);empty.Children.Add(paper);empty.Children.Add(AppStyle.Text("A little room to think.",18,false,true));empty.Children.Add(new TextBlock{Text="Start with Note, or try a Temp note.",FontSize=13,Foreground=AppStyle.B(AppStyle.P.Muted),Margin=new Thickness(0,10,0,0),TextWrapping=TextWrapping.Wrap});list.Children.Add(empty);
                }
            }
            foreach(var note in Store.Notes){var labels=rowText[note.Id];labels[0].Text=String.IsNullOrWhiteSpace(note.Title)?"Untitled":note.Title;string preview=(note.Body??"").Replace('\r',' ').Replace('\n',' ');if(preview.Length>130)preview=preview.Substring(0,130)+"…";labels[1].Text=String.IsNullOrWhiteSpace(preview)?"Empty note":preview;AutomationProperties.SetName(noteRows[note.Id],"Open "+labels[0].Text);}
            scroll.ScrollToVerticalOffset(offset);
        }
        private void ShowMenu(FrameworkElement anchor)
        {
            if(menu!=null)menu.IsOpen=false;var panel=MenuPanel();menu=new Popup{PlacementTarget=anchor,Placement=PlacementMode.Bottom,StaysOpen=false,AllowsTransparency=true,Child=panel};Motion.Menu(menu);menu.IsOpen=true;
        }
        private Border MenuPanel()
        {
            var p=new StackPanel{Width=280,Margin=new Thickness(5)};Border card=AppStyle.Card(p,12);card.Effect=new System.Windows.Media.Effects.DropShadowEffect{BlurRadius=18,ShadowDepth=5,Opacity=.16};
            AddMenu(p,"settings","Settings",()=>app.OpenSettings());AddMenu(p,"theme","Appearance",()=>AppStyle.ShowThemePicker(menu.PlacementTarget,app.ApplyThemeToWindows));var actions=new Grid{Margin=new Thickness(0,5,0,0)};actions.ColumnDefinitions.Add(new ColumnDefinition());actions.ColumnDefinitions.Add(new ColumnDefinition());var hide=AppStyle.MenuButton("hide","Hide to tray",()=>{if(menu!=null)menu.IsOpen=false;Hide();});var exit=AppStyle.MenuButton("exit","Exit Notes",()=>{if(menu!=null)menu.IsOpen=false;app.ExitApp();});Grid.SetColumn(exit,1);actions.Children.Add(hide);actions.Children.Add(exit);p.Children.Add(new Border{Height=1,Background=AppStyle.B(AppStyle.P.Border),Margin=new Thickness(10,5,10,0)});p.Children.Add(actions);return card;
        }
        private void AddMenu(StackPanel p,string icon,string text,Action action){var b=new Button{Content=new StackPanel{Orientation=Orientation.Horizontal,Children={Icons.Create(icon),new TextBlock{Text=text,FontSize=13,Foreground=AppStyle.B(AppStyle.P.Text),Margin=new Thickness(12,0,0,0),VerticalAlignment=VerticalAlignment.Center}}},Height=40,Padding=new Thickness(12,4,12,4),HorizontalContentAlignment=HorizontalAlignment.Left,Background=Brushes.Transparent,BorderThickness=new Thickness(0),Cursor=Cursors.Hand};b.Click+=(s,e)=>{if(menu!=null)menu.IsOpen=false;action();};AppStyle.DecorateButton(b);p.Children.Add(b);}
        private void ApplyAll(){app.ApplyThemeToWindows();}
        public void ApplyTheme(){double offset=scroll.VerticalOffset;Build();RefreshNotes();scroll.ScrollToVerticalOffset(offset);}
    }
    internal sealed class NoteWindow : Window
    {
        private readonly NotesApp app; private readonly Note note; private bool temporary; private bool initializing=true; private bool discardEdits; private TextBox title,body; private TextBlock status; private Button pin; private Border root; private Popup menu; private StackPanel links; private ScrollViewer editor;
        public NoteWindow(NotesApp a, Note n, bool temp)
        {
            app=a;note=n;temporary=temp;Topmost=n.AlwaysOnTop;Width=430;Height=420;MinWidth=300;MinHeight=240;WindowStartupLocation=WindowStartupLocation.CenterScreen;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.CanResize;ShowInTaskbar=false;Background=AppStyle.B(AppStyle.P.Background);AppStyle.Chrome(this);Build();
            if(n.Links==null)n.Links=new List<NoteLink>();title.Text=n.Title;body.Text=n.Body;RefreshLinks();
            initializing=false;Title=n.Title;
            AutomationProperties.SetName(title,"Note title");AutomationProperties.SetName(body,"Note content");Loaded+=(s,e)=>{if(n.Title=="Untitled"&&String.IsNullOrWhiteSpace(n.Body)){title.Focus();title.SelectAll();}else{body.Focus();body.CaretIndex=body.Text.Length;}};
            if(!temporary)app.FloatingChanged(note,true);
            Closing+=(s,e)=>{if(!discardEdits)Save();};
        }
        private void Build()
        {
            root=new Border{Background=AppStyle.B(AppStyle.P.Surface),BorderBrush=AppStyle.B(AppStyle.P.Border),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(15),Padding=new Thickness(24,18,24,20)};
            var grid=new Grid();grid.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});grid.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});grid.RowDefinitions.Add(new RowDefinition{Height=new GridLength(1,GridUnitType.Star)});
            var header=new Grid{Margin=new Thickness(0,0,0,12)};header.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});header.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});header.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});header.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
            title=new TextBox{Style=AppStyle.TextBox(true),FontSize=21,VerticalContentAlignment=VerticalAlignment.Center};title.TextChanged+=(s,e)=>ScheduleSave();header.Children.Add(title);
            pin=AppStyle.Button("Always on top",TogglePin,true);Grid.SetColumn(pin,1);header.Children.Add(pin);
            var more=AppStyle.Button("···",()=>OpenMenu((FrameworkElement)header.Children[2]),true);Grid.SetColumn(more,2);header.Children.Add(more);
            var close=AppStyle.Button("×",()=>Close(),true);close.ToolTip="Close note";Grid.SetColumn(close,3);header.Children.Add(close);
            header.MouseLeftButtonDown+=(s,e)=>{if(AppStyle.IsInput(e.OriginalSource as DependencyObject))return;if(e.ClickCount==2){WindowState=WindowState==WindowState.Maximized?WindowState.Normal:WindowState.Maximized;}else if(e.ButtonState==MouseButtonState.Pressed)try{DragMove();}catch{}};grid.Children.Add(header);
            status=AppStyle.Text(temporary?"Temporary":"",12,true);status.Margin=new Thickness(0,0,0,18);Grid.SetRow(status,1);grid.Children.Add(status);
            body=new TextBox{Style=AppStyle.TextBox(),FontSize=Store.Prefs.TextSize,AcceptsTab=true,VerticalScrollBarVisibility=ScrollBarVisibility.Disabled,MinHeight=90};body.TextChanged+=(s,e)=>ScheduleSave();
            var writing=new StackPanel();writing.Children.Add(body);links=new StackPanel{Margin=new Thickness(0,12,0,0)};writing.Children.Add(links);editor=new ScrollViewer{Content=writing,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};editor.SizeChanged+=(s,e)=>UpdateWritingHeight();Grid.SetRow(editor,2);grid.Children.Add(editor);
            body.PreviewMouseLeftButtonDown+=(s,e)=>{if((Keyboard.Modifiers&ModifierKeys.Control)!=0){int index=body.GetCharacterIndexFromPoint(e.GetPosition(body),true);string url=LinkTools.UrlAt(body.Text,index);if(url!=null){LinkTools.Open(url);e.Handled=true;}}};
            root.Child=grid;Content=root;AppStyle.DragSurface(this,root);UpdatePin();
            KeyDown+=(s,e)=>{if(e.Key==Key.Escape&&menu!=null)menu.IsOpen=false;if((Keyboard.Modifiers&ModifierKeys.Control)!=0&&e.Key==Key.K){EditLink(null);e.Handled=true;}};
        }
        private void ScheduleSave(){if(!initializing&&!discardEdits)Save();}
        private void Save(){if(initializing||discardEdits||title==null||body==null)return;note.Title=String.IsNullOrWhiteSpace(title.Text)?"Untitled":title.Text;note.Body=body.Text;Title=note.Title;app.NoteEdited(note,temporary);}
        private void TogglePin(){Topmost=!Topmost;note.AlwaysOnTop=Topmost;UpdatePin();Motion.Pin(pin.Content as FrameworkElement,Topmost);if(!temporary)app.NoteEdited(note,false);}
        private void UpdatePin(){if(pin==null)return;pin.Content=AppStyle.PinIcon(Topmost);pin.Foreground=AppStyle.B(Topmost?AppStyle.P.Accent:AppStyle.P.Muted);pin.Background=Topmost?AppStyle.B(AppStyle.P.Selected):Brushes.Transparent;pin.ToolTip=Topmost?"Always on top · on":"Always on top · off";AutomationProperties.SetName(pin,pin.ToolTip.ToString());}
        private void OpenMenu(FrameworkElement target){if(menu!=null)menu.IsOpen=false;var stack=new StackPanel{Width=218,Margin=new Thickness(5)};if(temporary)Add(stack,"save","Save note",()=>{Save();temporary=false;app.SaveAs(note);app.FloatingChanged(note,true);Motion.Saved(status);});Add(stack,"link","Add link…",()=>EditLink(null));Add(stack,"collection","All notes",()=>app.ShowMain());Add(stack,"theme","Appearance",()=>AppStyle.ShowThemePicker(menu.PlacementTarget,app.ApplyThemeToWindows));Add(stack,"settings","Settings",()=>app.OpenSettings());if(!temporary)Add(stack,"trash","Delete note",()=>{if(MessageBox.Show("Delete this saved note? This cannot be undone.","Delete note",MessageBoxButton.YesNo,MessageBoxImage.Warning)==MessageBoxResult.Yes)app.Remove(note);});Add(stack,"close","Close note",()=>Close());var card=AppStyle.Card(stack,12);card.Effect=new System.Windows.Media.Effects.DropShadowEffect{BlurRadius=18,ShadowDepth=5,Opacity=.18};menu=new Popup{PlacementTarget=target,Placement=PlacementMode.Bottom,StaysOpen=false,AllowsTransparency=true,Child=card};Motion.Menu(menu);menu.IsOpen=true;}
        private void Add(StackPanel stack,string icon,string label,Action action){var row=new StackPanel{Orientation=Orientation.Horizontal};row.Children.Add(Icons.Create(icon));row.Children.Add(new TextBlock{Text=label,FontSize=13,Foreground=AppStyle.B(AppStyle.P.Text),Margin=new Thickness(12,0,0,0),VerticalAlignment=VerticalAlignment.Center});var b=new Button{Content=row,Height=40,Padding=new Thickness(12,4,12,4),HorizontalContentAlignment=HorizontalAlignment.Left,Background=Brushes.Transparent,BorderThickness=new Thickness(0),Cursor=Cursors.Hand};b.Click+=(s,e)=>{if(menu!=null)menu.IsOpen=false;action();};AppStyle.DecorateButton(b);stack.Children.Add(b);}
        private void CycleTheme(){Store.Prefs.Theme=Store.Prefs.Theme=="Light"?"Dark":Store.Prefs.Theme=="Dark"?"Follow Windows":"Light";Store.SavePrefs();app.ApplyThemeToWindows();}
        public void ApplyTheme(){Scrollbars.Apply(this);Background=AppStyle.B(AppStyle.P.Background);root.Background=AppStyle.B(AppStyle.P.Surface);root.BorderBrush=AppStyle.B(AppStyle.P.Border);if(title!=null){title.Foreground=AppStyle.B(AppStyle.P.Text);body.Foreground=AppStyle.B(AppStyle.P.Text);title.CaretBrush=AppStyle.Caret;body.CaretBrush=AppStyle.Caret;status.Foreground=AppStyle.B(AppStyle.P.Muted);body.FontSize=Store.Prefs.TextSize;RefreshButtons(root);UpdatePin();RefreshLinks();}}
        private void UpdateWritingHeight(){if(body!=null)body.MinHeight=note.Links==null||note.Links.Count==0?Math.Max(90,editor.ActualHeight-4):90;}
        private void EditLink(NoteLink link){var dialog=new LinkEditor(this,link);if(dialog.ShowDialog()==true){if(link==null)note.Links.Add(dialog.Result);else{link.Url=dialog.Result.Url;link.Title=dialog.Result.Title;link.Text=dialog.Result.Text;}RefreshLinks();Save();}}
        private void RefreshLinks(){if(links==null||note.Links==null)return;links.Margin=note.Links.Count==0?new Thickness(0):new Thickness(0,12,0,0);links.Children.Clear();foreach(var item in note.Links){var link=item;var button=LinkTools.Row(link,()=>EditLink(link),()=>{if(MessageBox.Show("Remove this link from the note?","Remove link",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes){note.Links.Remove(link);RefreshLinks();Save();}});links.Children.Add(button);}UpdateWritingHeight();}
        public void CloseFromApp(){Close();}
        public void CloseForDeletion(){discardEdits=true;Close();}
        private void RefreshButtons(DependencyObject parent){for(int i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++){var child=VisualTreeHelper.GetChild(parent,i);var button=child as Button;if(button!=null){button.Foreground=AppStyle.B(AppStyle.P.Muted);AppStyle.DecorateButton(button);}else RefreshButtons(child);}}
    }

    internal sealed class SettingsWindow : Window
    {
        private readonly NotesApp app;private CheckBox launch;
        public SettingsWindow(NotesApp a){app=a;Title="Settings";Width=500;Height=660;MinWidth=410;MinHeight=490;WindowStartupLocation=WindowStartupLocation.CenterOwner;Window owner=Application.Current.MainWindow;if(owner!=null&&owner!=this&&owner.IsVisible)Owner=owner;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.CanResize;AppStyle.Chrome(this);Build();KeyDown+=(s,e)=>{if(e.Key==Key.Escape)Close();};}
        private void Build()
        {
            Scrollbars.Apply(this);Background=AppStyle.B(AppStyle.P.Surface);var panel=new StackPanel();var header=new DockPanel{Margin=new Thickness(0,0,0,16)};var close=AppStyle.Button("×",()=>Close(),true);DockPanel.SetDock(close,Dock.Right);header.Children.Add(close);header.Children.Add(AppStyle.Text("Settings",22,false,true));panel.Children.Add(header);
            panel.Children.Add(Label("Appearance"));panel.Children.Add(SettingsControls.Choices(new[]{"Light","Dark","Follow Windows"},Store.Prefs.Theme,choice=>{Store.Prefs.Theme=choice;Store.SavePrefs();app.ApplyThemeToWindows();Build();},true));
            panel.Children.Add(Label("Note text size"));panel.Children.Add(SettingsControls.Choices(new[]{"14","16","18","20","22","24"},((int)Store.Prefs.TextSize).ToString(),choice=>{Store.Prefs.TextSize=Convert.ToDouble(choice);Store.SavePrefs();app.ApplyThemeToWindows();}));
            panel.Children.Add(Label("Your workspace"));
            var temporary=SettingsControls.Switch("New notes start temporary",Store.Prefs.DefaultTemporary);temporary.Click+=(s,e)=>{Store.Prefs.DefaultTemporary=temporary.IsChecked==true;Store.SavePrefs();};panel.Children.Add(temporary);
            panel.Children.Add(new TextBlock{Text="Temporary notes disappear when you close them.",FontSize=11,Foreground=AppStyle.B(AppStyle.P.Muted),Margin=new Thickness(46,-3,0,8),TextWrapping=TextWrapping.Wrap});
            var restore=SettingsControls.Switch("Restore saved floating notes",Store.Prefs.RestoreFloatingNotes);restore.Click+=(s,e)=>{Store.Prefs.RestoreFloatingNotes=restore.IsChecked==true;Store.SavePrefs();};panel.Children.Add(restore);
            launch=SettingsControls.Switch("Launch quietly at startup",Store.Prefs.LaunchAtStartup);launch.Click+=(s,e)=>SetStartup(launch.IsChecked==true);panel.Children.Add(launch);
            var footer=new StackPanel{Margin=new Thickness(0,24,0,0)};footer.Children.Add(new Border{Height=1,Background=AppStyle.B(AppStyle.P.Border),Margin=new Thickness(0,0,0,18)});footer.Children.Add(AppStyle.Text("Made with \u2764\uFE0F by bhagyesh",14,false,true));footer.Children.Add(new TextBlock{Text="Infinite Licence for you.",Foreground=AppStyle.B(AppStyle.P.Muted),FontSize=12,Margin=new Thickness(0,5,0,0)});panel.Children.Add(footer);
            Content=new Border{Child=new ScrollViewer{Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled},Background=AppStyle.B(AppStyle.P.Surface),BorderBrush=AppStyle.B(AppStyle.P.Border),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(14),Padding=new Thickness(26)};
        }
        private TextBlock Label(string text){var label=AppStyle.Text(text,12,false,true);label.Margin=new Thickness(0,18,0,10);return label;}
        private void SetStartup(bool enabled)
        {
            try{using(var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run",true)){if(key==null)throw new InvalidOperationException("Windows startup settings are unavailable.");if(enabled)key.SetValue("Notes","\""+System.Reflection.Assembly.GetExecutingAssembly().Location+"\" --tray");else key.DeleteValue("Notes",false);}Store.Prefs.LaunchAtStartup=enabled;Store.SavePrefs();}
            catch(Exception ex){launch.IsChecked=Store.Prefs.LaunchAtStartup;MessageBox.Show("Windows couldn't update the startup setting. "+ex.Message,"Notes",MessageBoxButton.OK,MessageBoxImage.Warning);}
        }
    }
}
