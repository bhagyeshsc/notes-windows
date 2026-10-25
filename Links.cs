using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Automation;

namespace NotesApp
{
    public sealed class NoteLink
    {
        public string Id {get;set;}
        public string Url {get;set;}
        public string Title {get;set;}
        public string Text {get;set;}
        public NoteLink(){Id=Guid.NewGuid().ToString("N");Url="";Title="";Text="";}
    }
    internal static class LinkTools
    {
        public static string Normalize(string value)
        {
            value=(value??"").Trim();if(value.StartsWith("www.",StringComparison.OrdinalIgnoreCase))value="https://"+value;
            Uri uri;if(!Uri.TryCreate(value,UriKind.Absolute,out uri)||(uri.Scheme!="https"&&uri.Scheme!="http")||String.IsNullOrWhiteSpace(uri.Host))return null;return uri.AbsoluteUri;
        }
        public static string Label(NoteLink link){return !String.IsNullOrWhiteSpace(link.Text)?link.Text:!String.IsNullOrWhiteSpace(link.Title)?link.Title:link.Url;}
        public static string UrlAt(string text,int index)
        {
            foreach(Match match in Regex.Matches(text??"",@"https?://[^\s<>""]+",RegexOptions.IgnoreCase))if(index>=match.Index&&index<match.Index+match.Length)return Normalize(match.Value.TrimEnd('.',',',';',')',']','}'));
            return null;
        }
        public static void Open(string url)
        {
            string normalized=Normalize(url);if(normalized==null)return;
            try{Process.Start(new ProcessStartInfo(normalized){UseShellExecute=true});}catch(Exception ex){MessageBox.Show("The link couldn't be opened. "+ex.Message,"Open link",MessageBoxButton.OK,MessageBoxImage.Information);}
        }
        public static Button Row(NoteLink link,Action edit,Action remove)
        {
            var layout=new Grid();layout.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(25)});layout.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});
            var icon=Icons.Create("link",true,17);icon.VerticalAlignment=VerticalAlignment.Top;icon.Margin=new Thickness(0,3,0,0);layout.Children.Add(icon);
            var words=new StackPanel();words.Children.Add(new TextBlock{Text=Label(link),Foreground=AppStyle.B(AppStyle.P.Accent),FontSize=14,TextWrapping=TextWrapping.Wrap});
            if(Label(link)!=link.Url)words.Children.Add(new TextBlock{Text=link.Url,Foreground=AppStyle.B(AppStyle.P.Muted),FontSize=11,TextTrimming=TextTrimming.CharacterEllipsis,Margin=new Thickness(0,4,0,0)});Grid.SetColumn(words,1);layout.Children.Add(words);
            var row=new Button{Content=layout,Padding=new Thickness(10),Margin=new Thickness(-10,0,0,4),HorizontalContentAlignment=HorizontalAlignment.Stretch,Background=Brushes.Transparent,BorderThickness=new Thickness(0),Cursor=Cursors.Hand,ToolTip=String.IsNullOrWhiteSpace(link.Title)?link.Url:link.Title+"\n"+link.Url};
            AppStyle.DecorateButton(row);AutomationProperties.SetName(row,"Open link: "+Label(link));AutomationProperties.SetHelpText(row,"Press Shift+F10 to edit, copy, or remove this link.");row.Click+=(s,e)=>Open(link.Url);
            Action menu=()=>{Popup popup=null;var choices=new StackPanel{Width=185,Margin=new Thickness(5)};choices.Children.Add(AppStyle.MenuButton("edit","Edit link",()=>{popup.IsOpen=false;edit();}));choices.Children.Add(AppStyle.MenuButton("copy","Copy URL",()=>{Clipboard.SetText(link.Url);popup.IsOpen=false;}));choices.Children.Add(AppStyle.MenuButton("trash","Remove link",()=>{popup.IsOpen=false;remove();}));popup=new Popup{Child=AppStyle.Card(choices,12),PlacementTarget=row,Placement=PlacementMode.Bottom,StaysOpen=false,AllowsTransparency=true};Motion.Menu(popup);popup.IsOpen=true;};
            row.MouseRightButtonUp+=(s,e)=>{menu();e.Handled=true;};row.PreviewKeyDown+=(s,e)=>{if(e.Key==Key.Apps||(e.Key==Key.F10&&(Keyboard.Modifiers&ModifierKeys.Shift)!=0)){menu();e.Handled=true;}};return row;
        }
    }
    internal sealed class LinkEditor : Window
    {
        private TextBox url,title,text;private TextBlock error;private readonly bool editing;
        public NoteLink Result {get;private set;}
        public LinkEditor(Window owner,NoteLink link)
        {
            Owner=owner;editing=link!=null;Title=editing?"Edit link":"Add link";Width=440;Height=440;MinWidth=360;MinHeight=420;WindowStartupLocation=WindowStartupLocation.CenterOwner;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.CanResize;ShowInTaskbar=false;Background=AppStyle.B(AppStyle.P.Surface);AppStyle.Chrome(this);
            var panel=new StackPanel();var header=new DockPanel{Margin=new Thickness(0,0,0,14)};var close=AppStyle.Button("×",()=>Close(),true);DockPanel.SetDock(close,Dock.Right);header.Children.Add(close);header.Children.Add(AppStyle.Text(Title,22,false,true));panel.Children.Add(header);
            url=Field(panel,"URL",link==null?"":link.Url);title=Field(panel,"Title (optional)",link==null?"":link.Title);text=Field(panel,"Link text (optional)",link==null?"":link.Text);
            panel.Children.Add(new TextBlock{Text="Leave both optional fields blank to show the URL.",FontSize=12,Foreground=AppStyle.B(AppStyle.P.Muted),Margin=new Thickness(0,10,0,0),TextWrapping=TextWrapping.Wrap});
            error=new TextBlock{Foreground=AppStyle.B(AppStyle.P.Accent),FontSize=12,Margin=new Thickness(0,10,0,8),TextWrapping=TextWrapping.Wrap};panel.Children.Add(error);
            var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};actions.Children.Add(AppStyle.Button("Cancel",()=>Close()));var save=AppStyle.Button(editing?"Save changes":"Add link",Commit);save.Background=AppStyle.B(AppStyle.P.Selected);save.Foreground=AppStyle.B(AppStyle.P.Text);save.Margin=new Thickness(8,0,0,0);save.IsDefault=true;actions.Children.Add(save);panel.Children.Add(actions);
            Content=new Border{Child=new ScrollViewer{Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto},Padding=new Thickness(24),Background=AppStyle.B(AppStyle.P.Surface),BorderBrush=AppStyle.B(AppStyle.P.Border),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(14)};
            Loaded+=(s,e)=>url.Focus();KeyDown+=(s,e)=>{if(e.Key==Key.Escape)Close();};
        }
        private TextBox Field(StackPanel panel,string label,string value)
        {
            var caption=AppStyle.Text(label,12,true);caption.Margin=new Thickness(0,10,0,7);panel.Children.Add(caption);var field=new TextBox{Text=value??"",Style=AppStyle.TextBox(true),AcceptsReturn=false,FontSize=14,MinHeight=36,Padding=new Thickness(10,8,10,8),Background=AppStyle.B(AppStyle.P.Background),BorderThickness=new Thickness(1),BorderBrush=AppStyle.B(AppStyle.P.Border)};AutomationProperties.SetName(field,label);panel.Children.Add(field);return field;
        }
        private void Commit(){string normalized=LinkTools.Normalize(url.Text);if(normalized==null){error.Text="Enter a valid http:// or https:// URL.";url.Focus();return;}Result=new NoteLink{Url=normalized,Title=title.Text.Trim(),Text=text.Text.Trim()};DialogResult=true;}
    }
}
