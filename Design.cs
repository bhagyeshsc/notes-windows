using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace NotesApp
{
    internal static class Icons
    {
        public static FrameworkElement Create(string name, bool accent=false, double size=18)
        {
            Brush ink=AppStyle.B(accent?AppStyle.P.Accent:AppStyle.P.Muted);
            var canvas=new Canvas{Width=24,Height=24,IsHitTestVisible=false};
            if(name=="more"){
                foreach(double x in new[]{4.0,11.0,18.0}){var dot=new System.Windows.Shapes.Ellipse{Width=2.5,Height=2.5,Fill=ink};Canvas.SetLeft(dot,x);Canvas.SetTop(dot,10.75);canvas.Children.Add(dot);}
            }else{
                string data;
                switch(name){
                    case "pin": data="M8 3 H16 M9 3 V8 L6 12 V14 H18 V12 L15 8 V3 M12 14 V21";break;
                    case "plus": data="M12 5 V19 M5 12 H19";break;
                    case "new": data="M14 3 H6 Q4 3 4 5 V19 Q4 21 6 21 H18 Q20 21 20 19 V9 L14 3 M14 3 V9 H20 M8 15 H16 M12 11 V19";break;
                    case "temporary": data="M7 3 H17 Q20 3 20 6 V18 Q20 21 17 21 H7 Q4 21 4 18 V6 Q4 3 7 3 M8 8 H16 M8 12 H13 M16 15 V18 M14.5 16.5 H17.5";break;
                    case "save": data="M12 3 V15 M7 10 L12 15 L17 10 M4 15 V19 Q4 21 6 21 H18 Q20 21 20 19 V15";break;
                    case "collection": data="M7 3 H17 Q19 3 19 5 V17 Q19 19 17 19 H7 Q5 19 5 17 V5 Q5 3 7 3 M8 7 H16 M8 11 H14 M2 7 V20 Q2 22 4 22 H15";break;
                    case "theme": data="M20.5 14.1 A9 9 0 0 1 9.9 3.5 A9 9 0 1 0 20.5 14.1 Z";break;
                    case "sun": data="M16 12 A4 4 0 1 1 8 12 A4 4 0 1 1 16 12 M12 2 V4 M12 20 V22 M2 12 H4 M20 12 H22 M5 5 L6.5 6.5 M17.5 17.5 L19 19 M5 19 L6.5 17.5 M17.5 6.5 L19 5";break;
                    case "system": data="M4 4 H20 Q22 4 22 6 V15 Q22 17 20 17 H4 Q2 17 2 15 V6 Q2 4 4 4 M12 17 V21 M7 21 H17";break;
                    case "hide": data="M5 12 H19";break;
                    case "link": data="M10 14 L14 10 M8 15 L6 17 A4 4 0 0 1 1 12 L6 7 A4 4 0 0 1 11 7 M16 9 L18 7 A4 4 0 0 1 23 12 L18 17 A4 4 0 0 1 13 17";break;
                    case "edit": data="M15 4 L20 9 M4 20 L5 15 L16 4 Q18 2 20 4 Q22 6 20 8 L9 19 Z M13 20 H21";break;
                    case "copy": data="M9 8 H19 Q21 8 21 10 V20 Q21 22 19 22 H9 Q7 22 7 20 V10 Q7 8 9 8 M16 5 V4 Q16 2 14 2 H4 Q2 2 2 4 V14 Q2 16 4 16";break;
                    case "settings": data="M4 6 H8 M13 6 H20 M4 12 H14 M19 12 H20 M4 18 H6 M11 18 H20 M8 3 V9 H13 V3 Z M14 9 V15 H19 V9 Z M6 15 V21 H11 V15 Z";break;
                    case "trash": data="M4 6 H20 M9 6 V3 H15 V6 M6 6 L7 21 H17 L18 6 M10 10 V17 M14 10 V17";break;
                    case "close": data="M6 6 L18 18 M18 6 L6 18";break;
                    case "exit": data="M10 4 H5 Q3 4 3 6 V18 Q3 20 5 20 H10 M9 12 H21 M17 8 L21 12 L17 16";break;
                    case "open": data="M8 5 H19 V16 M19 5 L5 19";break;
                    case "check": data="M5 12 L10 17 L19 7";break;
                    default: data="M14 3 H6 Q4 3 4 5 V19 Q4 21 6 21 H18 Q20 21 20 19 V9 L14 3 M14 3 V9 H20 M8 13 H16 M8 17 H13";break;
                }
                canvas.Children.Add(new System.Windows.Shapes.Path{Data=Geometry.Parse(data),Stroke=ink,StrokeThickness=1.55,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round,StrokeLineJoin=PenLineJoin.Round,Fill=null});
            }
            return new Viewbox{Width=size,Height=size,Child=canvas,Stretch=Stretch.Uniform,IsHitTestVisible=false};
        }
    }

    internal static class Motion
    {
        private static readonly DependencyProperty WiredProperty=DependencyProperty.RegisterAttached("Wired",typeof(bool),typeof(Motion),new PropertyMetadata(false));
        public static bool Enabled { get { return SystemParameters.ClientAreaAnimation; } }
        private static CubicEase Ease { get { return new CubicEase{EasingMode=EasingMode.EaseOut}; } }
        private static void To(Animatable target,DependencyProperty property,double value,int milliseconds)
        {
            double from=(double)target.GetValue(property);
            target.SetValue(property,value);
            target.BeginAnimation(property,new DoubleAnimation(from,value,TimeSpan.FromMilliseconds(milliseconds)){EasingFunction=Ease});
        }
        private static void Opacity(UIElement target,double value,int milliseconds)
        {
            double from=target.Opacity;target.Opacity=value;
            target.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(from,value,TimeSpan.FromMilliseconds(milliseconds)){EasingFunction=Ease});
        }
        public static void Arrive(FrameworkElement content)
        {
            if(content==null)return;
            if(!Enabled){content.Opacity=.94;Opacity(content,1,60);return;}
            var scale=new ScaleTransform(.988,.988);var offset=new TranslateTransform(0,5);var transforms=new TransformGroup();transforms.Children.Add(scale);transforms.Children.Add(offset);
            content.RenderTransformOrigin=new Point(.5,.35);content.RenderTransform=transforms;content.Opacity=.88;
            To(scale,ScaleTransform.ScaleXProperty,1,210);To(scale,ScaleTransform.ScaleYProperty,1,210);To(offset,TranslateTransform.YProperty,0,210);Opacity(content,1,170);
        }
        public static void Menu(Popup popup)
        {
            popup.Opened+=(s,e)=>{var content=popup.Child as FrameworkElement;if(content==null)return;content.Opacity=.8;Opacity(content,1,Enabled?110:45);if(Enabled){var offset=new TranslateTransform(0,-3);content.RenderTransform=offset;To(offset,TranslateTransform.YProperty,0,140);}};
        }
        public static void Pin(FrameworkElement icon,bool active)
        {
            if(!Enabled||icon==null)return;icon.RenderTransformOrigin=new Point(.5,.5);var rotation=new RotateTransform(active?-14:10);icon.RenderTransform=rotation;To(rotation,RotateTransform.AngleProperty,0,190);
        }
        public static void Saved(TextBlock label)
        {
            label.Text="Saved to Notes";label.Foreground=AppStyle.B(AppStyle.P.Accent);label.BeginAnimation(UIElement.OpacityProperty,null);label.Opacity=1;
            var fade=new DoubleAnimation(1,0,TimeSpan.FromMilliseconds(220)){BeginTime=TimeSpan.FromMilliseconds(1500)};fade.Completed+=(s,e)=>{label.Text="";label.BeginAnimation(UIElement.OpacityProperty,null);label.Opacity=1;};label.BeginAnimation(UIElement.OpacityProperty,fade);
        }
        public static void Wire(Button button)
        {
            if((bool)button.GetValue(WiredProperty))return;button.SetValue(WiredProperty,true);button.RenderTransformOrigin=new Point(.5,.5);button.RenderTransform=new ScaleTransform(1,1);
            button.MouseEnter+=(s,e)=>Hover(button,true);button.MouseLeave+=(s,e)=>{Hover(button,false);Press(button,false);};
            button.GotKeyboardFocus+=(s,e)=>Hover(button,true);button.LostKeyboardFocus+=(s,e)=>Hover(button,button.IsMouseOver);
            button.PreviewMouseLeftButtonDown+=(s,e)=>Press(button,true);button.PreviewMouseLeftButtonUp+=(s,e)=>Press(button,false);button.LostMouseCapture+=(s,e)=>Press(button,false);
            button.PreviewKeyDown+=(s,e)=>{if(e.Key==Key.Space||e.Key==Key.Return)Press(button,true);};button.PreviewKeyUp+=(s,e)=>Press(button,false);
        }
        private static void Hover(Button button,bool over)
        {
            var surface=button.Template.FindName("hoverSurface",button) as UIElement;if(surface!=null)Opacity(surface,over?1:0,Enabled?(over?110:160):40);
            var grid=button.Content as Grid;if(grid!=null){var arrow=grid.FindName("openCue") as UIElement;if(arrow!=null)Opacity(arrow,over?.9:0,Enabled?140:40);}
        }
        private static void Press(Button button,bool pressed)
        {
            var scale=button.RenderTransform as ScaleTransform;if(scale==null)return;double target=pressed&&Enabled?(button.ActualWidth>80?.987:.94):1;To(scale,ScaleTransform.ScaleXProperty,target,pressed?80:140);To(scale,ScaleTransform.ScaleYProperty,target,pressed?80:140);
        }
    }

    internal static class Branding
    {
        public static System.Drawing.Icon TrayIcon()
        {
            using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("Notes.App.ico"))
            using(var icon=new System.Drawing.Icon(stream,32,32))return (System.Drawing.Icon)icon.Clone();
        }
        public static ImageSource WindowIcon()
        {
            using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("Notes.App.ico")){
                var decoder=new IconBitmapDecoder(stream,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad);BitmapFrame frame=decoder.Frames[0];foreach(var candidate in decoder.Frames)if(candidate.PixelWidth>frame.PixelWidth)frame=candidate;frame.Freeze();return frame;
            }
        }
    }
    internal sealed class TrayColors : System.Windows.Forms.ProfessionalColorTable
    {
        private System.Drawing.Color Surface {get{return System.Drawing.ColorTranslator.FromHtml(AppStyle.P.Surface);}}
        private System.Drawing.Color Hover {get{return System.Drawing.ColorTranslator.FromHtml(AppStyle.P.Hover);}}
        public override System.Drawing.Color ToolStripDropDownBackground{get{return Surface;}}
        public override System.Drawing.Color MenuBorder{get{return System.Drawing.ColorTranslator.FromHtml(AppStyle.P.Border);}}
        public override System.Drawing.Color MenuItemSelected{get{return Hover;}}
        public override System.Drawing.Color MenuItemBorder{get{return Hover;}}
        public override System.Drawing.Color MenuItemSelectedGradientBegin{get{return Hover;}}
        public override System.Drawing.Color MenuItemSelectedGradientEnd{get{return Hover;}}
        public override System.Drawing.Color ImageMarginGradientBegin{get{return Surface;}}
        public override System.Drawing.Color ImageMarginGradientMiddle{get{return Surface;}}
        public override System.Drawing.Color ImageMarginGradientEnd{get{return Surface;}}
        public override System.Drawing.Color SeparatorDark{get{return MenuBorder;}}
        public override System.Drawing.Color SeparatorLight{get{return Surface;}}
    }
    internal static class SettingsControls
    {
        public static FrameworkElement Choices(string[] values,string selected,Action<string> changed,bool appearance=false)
        {
            var row=new UniformGrid{Columns=values.Length,Margin=new Thickness(-3,0,-3,0)};
            foreach(string value in values){string choice=value;var button=new Button{Content=value,Height=40,Margin=new Thickness(3,0,3,0),Padding=new Thickness(8),Background=AppStyle.B(value==selected?AppStyle.P.Selected:AppStyle.P.Background),Foreground=AppStyle.B(AppStyle.P.Text),BorderThickness=new Thickness(0),FontSize=13,Cursor=Cursors.Hand};
                if(appearance){var words=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center};words.Children.Add(Icons.Create(value=="Light"?"sun":value=="Dark"?"theme":"system",value==selected,16));var label=AppStyle.Text(value=="Follow Windows"?"Windows":value,12);label.Margin=new Thickness(7,0,0,0);words.Children.Add(label);button.Content=words;}
                AppStyle.DecorateButton(button);System.Windows.Automation.AutomationProperties.SetName(button,value);button.Click+=(s,e)=>{foreach(Button sibling in row.Children)sibling.Background=AppStyle.B(sibling==button?AppStyle.P.Selected:AppStyle.P.Background);changed(choice);};row.Children.Add(button);
            }return row;
        }
        public static CheckBox Switch(string label,bool value)
        {
            var toggle=new CheckBox{Content=label,IsChecked=value,Foreground=AppStyle.B(AppStyle.P.Text),FontSize=13,MinHeight=40,Cursor=Cursors.Hand,VerticalContentAlignment=VerticalAlignment.Center,FocusVisualStyle=null};
            var panel=new FrameworkElementFactory(typeof(DockPanel));var track=new FrameworkElementFactory(typeof(Grid));track.SetValue(FrameworkElement.WidthProperty,34.0);track.SetValue(FrameworkElement.HeightProperty,20.0);track.SetValue(FrameworkElement.MarginProperty,new Thickness(0,0,12,0));track.SetValue(DockPanel.DockProperty,Dock.Left);
            var trackFill=new FrameworkElementFactory(typeof(Border));trackFill.Name="track";trackFill.SetValue(Border.CornerRadiusProperty,new CornerRadius(10));trackFill.SetValue(Border.BackgroundProperty,AppStyle.B(AppStyle.P.Track));track.AppendChild(trackFill);
            var thumb=new FrameworkElementFactory(typeof(System.Windows.Shapes.Ellipse));thumb.Name="thumb";thumb.SetValue(FrameworkElement.WidthProperty,14.0);thumb.SetValue(FrameworkElement.HeightProperty,14.0);thumb.SetValue(FrameworkElement.HorizontalAlignmentProperty,HorizontalAlignment.Left);thumb.SetValue(FrameworkElement.MarginProperty,new Thickness(3,0,0,0));thumb.SetValue(System.Windows.Shapes.Shape.FillProperty,Brushes.White);thumb.SetValue(UIElement.RenderTransformProperty,new TranslateTransform());track.AppendChild(thumb);panel.AppendChild(track);
            var text=new FrameworkElementFactory(typeof(ContentPresenter));text.SetValue(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Center);panel.AppendChild(text);
            var template=new ControlTemplate(typeof(CheckBox)){VisualTree=panel};var check=new Trigger{Property=ToggleButton.IsCheckedProperty,Value=true};check.Setters.Add(new Setter(Border.BackgroundProperty,AppStyle.B(AppStyle.P.Accent),"track"));check.Setters.Add(new Setter(System.Windows.Shapes.Shape.FillProperty,AppStyle.B(AppStyle.P.AccentText),"thumb"));
            var on=new Storyboard();var go=new DoubleAnimation(14,TimeSpan.FromMilliseconds(Motion.Enabled?130:0));Storyboard.SetTargetName(go,"thumb");Storyboard.SetTargetProperty(go,new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));on.Children.Add(go);check.EnterActions.Add(new BeginStoryboard{Storyboard=on});
            var off=new Storyboard();var back=new DoubleAnimation(0,TimeSpan.FromMilliseconds(Motion.Enabled?100:0));Storyboard.SetTargetName(back,"thumb");Storyboard.SetTargetProperty(back,new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));off.Children.Add(back);check.ExitActions.Add(new BeginStoryboard{Storyboard=off});template.Triggers.Add(check);
            var focus=new Trigger{Property=UIElement.IsKeyboardFocusedProperty,Value=true};focus.Setters.Add(new Setter(Border.BorderBrushProperty,AppStyle.B(AppStyle.P.Text),"track"));focus.Setters.Add(new Setter(Border.BorderThicknessProperty,new Thickness(1),"track"));template.Triggers.Add(focus);toggle.Template=template;System.Windows.Automation.AutomationProperties.SetName(toggle,label);return toggle;
        }
    }
}
