using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
class BuildAssets
{
    static GraphicsPath Rounded(float x,float y,float width,float height,float radius){var p=new GraphicsPath();float d=radius*2;p.AddArc(x,y,d,d,180,90);p.AddArc(x+width-d,y,d,d,270,90);p.AddArc(x+width-d,y+height-d,d,d,0,90);p.AddArc(x,y+height-d,d,d,90,90);p.CloseFigure();return p;}
    static int Main(string[] args)
    {
        var sizes=new[]{16,24,32,48,64,128,256};var images=new List<byte[]>();
        foreach(int size in sizes)using(var bitmap=new Bitmap(size,size,PixelFormat.Format32bppArgb)){
            using(var g=Graphics.FromImage(bitmap)){
                g.SmoothingMode=SmoothingMode.AntiAlias;g.PixelOffsetMode=PixelOffsetMode.HighQuality;g.Clear(Color.Transparent);g.ScaleTransform(size/64F,size/64F);
                using(var tile=Rounded(1,1,62,62,14))using(var fill=new SolidBrush(Color.FromArgb(35,34,32)))using(var rim=new Pen(Color.FromArgb(75,72,66),1)){g.FillPath(fill,tile);g.DrawPath(rim,tile);}
                var paper=new GraphicsPath();paper.AddLines(new[]{new PointF(19,12),new PointF(37,12),new PointF(47,22),new PointF(47,49)});paper.AddArc(41,46,6,6,0,90);paper.AddLine(44,52,20,52);paper.AddArc(17,46,6,6,90,90);paper.AddLine(17,49,17,15);paper.AddArc(17,12,6,6,180,90);paper.CloseFigure();using(var fill=new SolidBrush(Color.FromArgb(247,242,230)))g.FillPath(fill,paper);paper.Dispose();
                using(var fold=new SolidBrush(Color.FromArgb(221,172,80)))g.FillPolygon(fold,new[]{new PointF(37,12),new PointF(47,22),new PointF(39,22),new PointF(37,20)});
                using(var line=new Pen(Color.FromArgb(136,127,111),2)){line.StartCap=LineCap.Round;line.EndCap=LineCap.Round;g.DrawLine(line,24,31,40,31);g.DrawLine(line,24,38,37,38);g.DrawLine(line,24,45,32,45);}
            }
            using(var memory=new MemoryStream()){bitmap.Save(memory,ImageFormat.Png);images.Add(memory.ToArray());}
            if(size==256)bitmap.Save(Path.Combine(Path.GetDirectoryName(args[0]),"app-icon-preview.png"),ImageFormat.Png);
        }
        using(var file=File.Create(args[0]))using(var writer=new BinaryWriter(file)){
            writer.Write((short)0);writer.Write((short)1);writer.Write((short)sizes.Length);int offset=6+16*sizes.Length;
            for(int i=0;i<sizes.Length;i++){writer.Write((byte)(sizes[i]==256?0:sizes[i]));writer.Write((byte)(sizes[i]==256?0:sizes[i]));writer.Write((byte)0);writer.Write((byte)0);writer.Write((short)1);writer.Write((short)32);writer.Write(images[i].Length);writer.Write(offset);offset+=images[i].Length;}
            foreach(var image in images)writer.Write(image);
        }
        return 0;
    }
}
