﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SGM4711_Eva.MDUserCtrls
{
    public partial class Multiply : UserControl
    {
        public Multiply()
        {
            InitializeComponent();
        }

        /***************************************************************************************
         *  属性值	                含义
         *  CategoryAttribute	    该属性对在Property控件中的属性按字母顺序进行归类
         *  DescriptionAttribute	其值为对每个属性的具体文字描述，将会显示在property控件的底部
         *  BrowsableAttribute	    该值为是否在property控件中显示或者隐藏某个属性
         *  ReadOnlyAttribute	    该值为某个属性值是否在property控件中只读
         *  DefaultValueAttribute	每个属性的默认值
         ***************************************************************************************/

        private Color drawColor = Color.SteelBlue;
        [CategoryAttribute("Style Settings"), DescriptionAttribute("What color of this multiply control")]
        public Color DrawColor
        {
            get { return this.drawColor; }
            set { this.drawColor = value; this.Refresh(); }
        }

        private void myPanel_Paint(object sender, PaintEventArgs e)
        {

            /*
             Graphics gra = this.pictureBox1.CreateGraphics();
            gra.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Brush bush = new SolidBrush(Color.Green);//填充的颜色
            gra.FillEllipse(bush, 10, 10, 100, 100);//画填充椭圆的方法，x坐标、y坐标、宽、高，如果是100，则半径为50

            画圆圈：
            Graphics gra = this.pictureBox1.CreateGraphics();
            gra.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Pen pen = new Pen(Color.Pink);//画笔颜色
            gra.DrawEllipse(pen, 250, 10, 100, 100);//画椭圆的方法，x坐标、y坐标、宽、高，如果是100，则半径为50
             */
            Pen myPen = new Pen(drawColor);

            // draw circle
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.DrawEllipse(myPen, new Rectangle(1, 1, this.Width - 2, this.Height - 2));

            // draw add
            e.Graphics.TranslateTransform(this.Width / 2, this.Height / 2); 
            e.Graphics.RotateTransform(45);
            e.Graphics.DrawLine(myPen, new Point(-(int)(this.Width - 2) / 2, 0),
                new Point((int)(this.Width - 2) / 2, 0));
            e.Graphics.DrawLine(myPen, new Point(0, (int)(this.Height - 2) / 2),
                new Point(0, -(int)(this.Height - 2) / 2));
        }

        private void Indicator_SizeChanged(object sender, EventArgs e)
        {
            this.Height = this.Width;
        }

        private void myPanel_Click(object sender, EventArgs e)
        {
            base.InvokeOnClick(this, e);
        }
    }
}
