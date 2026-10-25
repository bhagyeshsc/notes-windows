using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;

namespace NotesApp
{
    internal static class Scrollbars
    {
        private static readonly Dictionary<string,Style> Styles=new Dictionary<string,Style>();
        public static void Apply(Window window)
        {
            if(SystemParameters.HighContrast){window.Resources.Remove(typeof(ScrollBar));return;}
            Style style;if(!Styles.TryGetValue(AppStyle.P.Surface,out style)){style=(Style)XamlReader.Parse(Markup.Replace("$INK",AppStyle.P.Muted).Replace("$ACCENT",AppStyle.P.Accent).Replace("$REST",AppStyle.P.Surface=="#171717"?"0.60":"0.76").Replace("$TIME",Motion.Enabled?"0:0:0.12":"0:0:0"));style.Setters.Add(new EventSetter(FrameworkElement.LoadedEvent,new RoutedEventHandler(FitWidth)));Styles[AppStyle.P.Surface]=style;}window.Resources[typeof(ScrollBar)]=style;
        }
        private static void FitWidth(object sender,RoutedEventArgs args){var bar=(ScrollBar)sender;bar.ClearValue(bar.Orientation==System.Windows.Controls.Orientation.Vertical?FrameworkElement.WidthProperty:FrameworkElement.HeightProperty);}
        private const string Markup=@"
<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='{x:Type ScrollBar}'>
 <Setter Property='Width' Value='12'/><Setter Property='MinWidth' Value='0'/><Setter Property='MinHeight' Value='0'/><Setter Property='Background' Value='Transparent'/><Setter Property='Focusable' Value='False'/>
 <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='{x:Type ScrollBar}'>
  <ControlTemplate.Resources>
   <ControlTemplate x:Key='PageArea' TargetType='{x:Type RepeatButton}'><Border Background='Transparent'/></ControlTemplate>
   <ControlTemplate x:Key='VerticalPill' TargetType='{x:Type Thumb}'>
    <Grid Background='Transparent'><Border x:Name='Pill' Background='$INK' Width='5' Margin='0,2' HorizontalAlignment='Center' CornerRadius='4' Opacity='$REST'/></Grid>
    <ControlTemplate.Triggers>
     <Trigger Property='IsMouseOver' Value='True'><Setter TargetName='Pill' Property='Width' Value='7'/><Trigger.EnterActions><BeginStoryboard><Storyboard><DoubleAnimation Storyboard.TargetName='Pill' Storyboard.TargetProperty='Opacity' To='0.98' Duration='$TIME'/></Storyboard></BeginStoryboard></Trigger.EnterActions><Trigger.ExitActions><BeginStoryboard><Storyboard><DoubleAnimation Storyboard.TargetName='Pill' Storyboard.TargetProperty='Opacity' To='$REST' Duration='$TIME'/></Storyboard></BeginStoryboard></Trigger.ExitActions></Trigger>
     <Trigger Property='IsDragging' Value='True'><Setter TargetName='Pill' Property='Background' Value='$ACCENT'/><Setter TargetName='Pill' Property='Width' Value='7'/></Trigger>
    </ControlTemplate.Triggers>
   </ControlTemplate>
   <ControlTemplate x:Key='HorizontalPill' TargetType='{x:Type Thumb}'>
    <Grid Background='Transparent'><Border x:Name='Pill' Background='$INK' Height='5' Margin='2,0' VerticalAlignment='Center' CornerRadius='4' Opacity='$REST'/></Grid>
    <ControlTemplate.Triggers>
     <Trigger Property='IsMouseOver' Value='True'><Setter TargetName='Pill' Property='Height' Value='7'/><Trigger.EnterActions><BeginStoryboard><Storyboard><DoubleAnimation Storyboard.TargetName='Pill' Storyboard.TargetProperty='Opacity' To='0.98' Duration='$TIME'/></Storyboard></BeginStoryboard></Trigger.EnterActions><Trigger.ExitActions><BeginStoryboard><Storyboard><DoubleAnimation Storyboard.TargetName='Pill' Storyboard.TargetProperty='Opacity' To='$REST' Duration='$TIME'/></Storyboard></BeginStoryboard></Trigger.ExitActions></Trigger>
     <Trigger Property='IsDragging' Value='True'><Setter TargetName='Pill' Property='Background' Value='$ACCENT'/><Setter TargetName='Pill' Property='Height' Value='7'/></Trigger>
    </ControlTemplate.Triggers>
   </ControlTemplate>
  </ControlTemplate.Resources>
  <Grid Background='Transparent'>
   <Track x:Name='PART_Track' IsDirectionReversed='True' Orientation='{TemplateBinding Orientation}' Minimum='{TemplateBinding Minimum}' Maximum='{TemplateBinding Maximum}' ViewportSize='{TemplateBinding ViewportSize}' Value='{Binding Value,RelativeSource={RelativeSource TemplatedParent},Mode=TwoWay}'>
    <Track.DecreaseRepeatButton><RepeatButton x:Name='Before' Command='{x:Static ScrollBar.PageUpCommand}' Focusable='False' IsTabStop='False' Template='{StaticResource PageArea}'/></Track.DecreaseRepeatButton>
    <Track.Thumb><Thumb x:Name='Handle' MinHeight='30' Focusable='False' IsTabStop='False' Cursor='Arrow' Template='{StaticResource VerticalPill}'/></Track.Thumb>
    <Track.IncreaseRepeatButton><RepeatButton x:Name='After' Command='{x:Static ScrollBar.PageDownCommand}' Focusable='False' IsTabStop='False' Template='{StaticResource PageArea}'/></Track.IncreaseRepeatButton>
   </Track>
  </Grid>
  <ControlTemplate.Triggers>
   <Trigger Property='Orientation' Value='Horizontal'>
    <Setter Property='Width' Value='Auto'/><Setter Property='Height' Value='12'/><Setter TargetName='PART_Track' Property='IsDirectionReversed' Value='False'/>
    <Setter TargetName='Handle' Property='MinHeight' Value='0'/><Setter TargetName='Handle' Property='MinWidth' Value='30'/><Setter TargetName='Handle' Property='Template' Value='{StaticResource HorizontalPill}'/>
    <Setter TargetName='Before' Property='Command' Value='{x:Static ScrollBar.PageLeftCommand}'/><Setter TargetName='After' Property='Command' Value='{x:Static ScrollBar.PageRightCommand}'/>
   </Trigger>
   <Trigger Property='IsEnabled' Value='False'><Setter Property='Opacity' Value='0'/></Trigger>
  </ControlTemplate.Triggers>
 </ControlTemplate></Setter.Value></Setter>
</Style>";
    }
}
