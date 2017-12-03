﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MD.MDCommon;

namespace SGM4711_Eva.MDUserCtrls
{
    public partial class DRCCtrl : UserControl
    {
        #region Params
        List<Register> regList;
        string bf_DRC_EN;
        IRegOperation myRegOp;

        uint mask_3p23 = 0x3FFFFFF;
        uint mask_2p23 = (uint)Math.Pow(2, 23);

        int fs = 48000;
        // 3.23 formate
        uint ae;
        uint we;
        uint aa;
        uint wa;
        uint ad;
        uint wd;

        double threshold = -4;
        uint T_ThresholddB;   // 9.17 formate
        double slope = 1;
        uint K_slopedB;       // 3.23 formate, slope
        double offset = 0;
        uint O_offsetdB;      // 9.17 formate, offset
        bool DRC_EN;

        // Draw parameters
        double linearLenRatio = 0.6;
        double maxLenRatio = 0.9;
        int xAxis_Shift = 30;
        int yAxis_Shift = 30;
        int arrowSize = 20;

        double uintLen_1dB;
                
        Pen pen_smallGrid = new Pen(Color.LightGray, 1);
        //Pen pen_largeGrid = new Pen(Color.Gray, 1);
        static int frameWidth = 2;
        Pen pen_Frame = new Pen(Color.Gray, frameWidth);
        Brush brush_string = Brushes.DarkBlue;
        List<Point> largerGrid_L = new List<Point> { };
        List<Point> largerGrid_R = new List<Point> { };
        List<Point> largerGrid_U = new List<Point> { };
        List<Point> largerGrid_D = new List<Point> { };

        List<String> labelStr_L = new List<String> { };
        List<Point> label_Loc_L = new List<Point> { };
        List<String> labelStr_D = new List<String> { };
        List<Point> label_Loc_D = new List<Point> { };

        List<Point> smallGrid_L1 = new List<Point> { };
        List<Point> smallGrid_L2 = new List<Point> { };
        List<Point> smallGrid_R1 = new List<Point> { };
        List<Point> smallGrid_R2 = new List<Point> { };
        List<Point> smallGrid_U1 = new List<Point> { };
        List<Point> smallGrid_U2 = new List<Point> { };
        List<Point> smallGrid_D1 = new List<Point> { };
        List<Point> smallGrid_D2 = new List<Point> { };

        Point minPoint;
        Point maxPoint;
        Point centerPoint;
        int circleRadius = 5;
        double maxOffset = 24;
        double maxInOut = 24;
        double minInOut = -140;

        #endregion

        #region Funcs
        public DRCCtrl()
        {
            InitializeComponent();
        }

        public DRCCtrl(List<Register> _regList, string _DRCEN, IRegOperation _myRegOp)
        {
            InitializeComponent();
            this.regList = _regList;
            this.bf_DRC_EN = _DRCEN;

            myRegOp = _myRegOp;
            // Update GUI setting from register value
            //InitGUISetting();
            CalcBGDrawPoints();
            InitCurvePoints();    // should after CalcBGDrawPoints()
        }

        private void InitGUISetting()
        {
            /* Address DRC1: 0X3A, 0X3B, 0x3C; DRC2: 0X3D, 0x3E and 0x3F */
            // Energy time
            this.numUP_EnergyTime.ValueChanged -= numUP_EnergyTime_ValueChanged;
            this.numUP_EnergyTime.Value = (decimal)CalcTimeConstant(regList[0]["(1-ae)[25:0]"].BFValue);
            this.numUP_EnergyTime.ValueChanged += numUP_EnergyTime_ValueChanged;

            // Attack time
            this.numUP_AttackTime.ValueChanged -= numUP_AttackTime_ValueChanged;
            this.numUP_AttackTime.Value = (decimal)CalcTimeConstant(regList[1]["(1-ae)[25:0]"].BFValue);
            this.numUP_AttackTime.ValueChanged += numUP_AttackTime_ValueChanged;
            
            // Decay time
            this.numUP_DecayTime.ValueChanged -= numUP_DecayTime_ValueChanged;
            this.numUP_DecayTime.Value = (decimal)CalcTimeConstant(regList[2]["(1-ae)[25:0]"].BFValue);
            this.numUP_DecayTime.ValueChanged += numUP_DecayTime_ValueChanged;

            /* Threshold T, Address DRC1: 0X40; DRC2: 0X43 */
            this.numUP_Threshold.ValueChanged -= numUP_Threshold_ValueChanged;
            this.numUP_Threshold.Value = (decimal)CalcThreshold(regList[3].RegValue);
            this.numUP_Threshold.ValueChanged += numUP_Threshold_ValueChanged;

            /* Slope parameter K, Address DRC1: 0X41, DRC2: 0X44 */
            this.numUP_Slope.ValueChanged -= numUP_Slope_ValueChanged;
            this.numUP_Slope.Value = (decimal)CalcSlope(regList[4].RegValue);
            this.numUP_Slope.ValueChanged += numUP_Slope_ValueChanged;

            /* Offset, Address DRC1: 0X41, DRC2: 0X44 */
            this.numUP_Offset.ValueChanged -= numUP_Offset_ValueChanged;
            this.numUP_Offset.Value = (decimal)CalcOffset(regList[5].RegValue);
            this.numUP_Offset.ValueChanged += numUP_Offset_ValueChanged;

            /* DRC EN, Address 0x46, DRC_EN*/
            this.chb_Enable.Checked = regList[6][bf_DRC_EN].BFValue == 0 ? false : true;
        }

        /// <summary>
        /// Calcuate all the points for back ground. like grids, label, farmework
        /// </summary>
        private void CalcBGDrawPoints()
        {
            Graphics tempGraphic = this.myPanel.CreateGraphics();
            //SizeF tempSize = tempGraphic.MeasureString(MinFreq.ToString(), this.Font);
            
            // Max +24dB, Min -140dB
            uintLen_1dB = (this.myPanel.Width - xAxis_Shift - xAxis_Shift / 2) / (24d + 140);
            int smallGridLen = 5;
            int labelLocOffsetX = 2;
            int labelLocOffsetY = 5;
            string tempStr = "";
            SizeF tempSize;

            /* Small grid X */
            smallGrid_L1.Clear();
            smallGrid_L2.Clear();
            smallGrid_R1.Clear();
            smallGrid_R2.Clear();
            for (int ix = 20; ix > -140; ix -= 5)
            {
                smallGrid_L1.Add(new Point(xAxis_Shift, (int)(yAxis_Shift / 2 + uintLen_1dB * (24 - ix))));
                smallGrid_L2.Add(new Point(xAxis_Shift + smallGridLen, (int)(yAxis_Shift / 2 + uintLen_1dB * (24 - ix))));
                smallGrid_R1.Add(new Point(myPanel.Width - xAxis_Shift / 2 - smallGridLen, (int)(yAxis_Shift / 2 + uintLen_1dB * (24 - ix))));
                smallGrid_R1.Add(new Point(myPanel.Width - xAxis_Shift / 2, (int)(yAxis_Shift / 2 + uintLen_1dB * (24 - ix))));
            }

            /* Small grid Y */
            smallGrid_U1.Clear();
            smallGrid_U2.Clear();
            smallGrid_D1.Clear();
            smallGrid_D2.Clear();
            for (int ix = 20; ix > -140; ix -= 5)
            {
                smallGrid_U1.Add(new Point((int)(this.Width - xAxis_Shift / 2 - uintLen_1dB * (24 - ix)), yAxis_Shift / 2));
                smallGrid_U2.Add(new Point((int)(this.Width - xAxis_Shift / 2 - uintLen_1dB * (24 - ix)), yAxis_Shift / 2 + smallGridLen));
                smallGrid_D1.Add(new Point((int)(this.Width - xAxis_Shift / 2 - uintLen_1dB * (24 - ix)), myPanel.Height - yAxis_Shift));
                smallGrid_D2.Add(new Point((int)(this.Width - xAxis_Shift / 2 - uintLen_1dB * (24 - ix)), myPanel.Height - yAxis_Shift - smallGridLen));
            }

            /* Larger grid X and label */
            largerGrid_L.Clear();
            largerGrid_R.Clear();
            labelStr_L.Clear();
            label_Loc_L.Clear();

            //+24dB, -140dB label and Output
            //tempStr = "Out";
            //tempSize = tempGraphic.MeasureString(tempStr.ToString(), this.Font);
            //labelStr_L.Add(tempStr);
            //label_Loc_L.Add(new Point((int)(xAxis_Shift - tempSize.Width / 2), (int)(yAxis_Shift / 4 - tempSize.Height / 2)));

            tempStr = "24";
            tempSize = tempGraphic.MeasureString(tempStr.ToString(), this.Font);
            labelStr_L.Add(tempStr);
            label_Loc_L.Add(new Point((int)(xAxis_Shift - labelLocOffsetX - tempSize.Width), (int)(yAxis_Shift / 2 - tempSize.Height / 2)));

            tempStr = "-140";
            tempSize = tempGraphic.MeasureString(tempStr.ToString(), this.Font);
            labelStr_L.Add(tempStr);
            label_Loc_L.Add(new Point((int)(xAxis_Shift - labelLocOffsetX - tempSize.Width), (int)(myPanel.Height - yAxis_Shift - tempSize.Height / 2)));

            for (int ix = 0; ix > -140; ix -= 25)
            {
                largerGrid_L.Add(new Point(xAxis_Shift, (int)(yAxis_Shift / 2 + uintLen_1dB * (24 - ix))));
                largerGrid_R.Add(new Point(myPanel.Width - xAxis_Shift / 2, (int)(yAxis_Shift / 2 + uintLen_1dB * (24 - ix))));

                // label location
                tempStr = ix.ToString();
                tempSize = tempGraphic.MeasureString(tempStr.ToString(), this.Font);
                labelStr_L.Add(tempStr);
                label_Loc_L.Add(new Point((int)(xAxis_Shift - labelLocOffsetX - tempSize.Width), (int)(yAxis_Shift / 2 + uintLen_1dB * (24 - ix) - tempSize.Height / 2)));
            }

            /* Larger grid Y */
            largerGrid_U.Clear();
            largerGrid_D.Clear();
            labelStr_D.Clear();
            label_Loc_D.Clear();

            //+24dB, -140dB label and Input
            //tempStr = "In";
            //tempSize = tempGraphic.MeasureString(tempStr.ToString(), this.Font);
            //labelStr_D.Add(tempStr);
            //label_Loc_D.Add(new Point((int)(myPanel.Width - tempSize.Width), (int)(this.myPanel.Height - yAxis_Shift - tempSize.Height / 2)));

            tempStr = "24";
            tempSize = tempGraphic.MeasureString(tempStr.ToString(), this.Font);
            labelStr_D.Add(tempStr);
            label_Loc_D.Add(new Point((int)(myPanel.Width - xAxis_Shift / 2 - tempSize.Width / 2), myPanel.Height - yAxis_Shift + labelLocOffsetY));

            tempStr = "-140";
            tempSize = tempGraphic.MeasureString(tempStr.ToString(), this.Font);
            labelStr_D.Add(tempStr);
            label_Loc_D.Add(new Point((int)(xAxis_Shift - tempSize.Width / 2), myPanel.Height - yAxis_Shift + labelLocOffsetY));

            for (int ix = 0; ix > -140; ix -= 25)
            {
                largerGrid_U.Add(new Point((int)(this.Width - xAxis_Shift / 2 - uintLen_1dB * (24 - ix)), yAxis_Shift / 2));
                largerGrid_D.Add(new Point((int)(this.Width - xAxis_Shift / 2 - uintLen_1dB * (24 - ix)), myPanel.Height - yAxis_Shift));

                // label location
                tempStr = ix.ToString();
                tempSize = tempGraphic.MeasureString(tempStr.ToString(), this.Font);
                labelStr_D.Add(tempStr);
                label_Loc_D.Add(new Point((int)(this.Width - xAxis_Shift / 2 - uintLen_1dB * (24 - ix) - tempSize.Width / 2), myPanel.Height - yAxis_Shift + labelLocOffsetY));
            }
        }

        /// <summary>
        /// Init the DRC curve points.All the calculation based on below TranslateTransform func. 
        /// work zone value range: x [0, uintLen_1dB * 164], y [-1 * uintLen_1dB * 164, 0]
        /// e.Graphics.TranslateTransform(xAxis_Shift, this.myPanel.Height - yAxis_Shift);
        /// </summary>
        private void InitCurvePoints()
        {
            // min Point
            if (offset <= 0)
            {
                minPoint = new Point((int)(-1 * uintLen_1dB * offset), 0);
            }
            else  // y will be a negative value
            {
                minPoint = new Point(0 ,(int)(-1 * uintLen_1dB * offset));
            }

            // center point
            centerPoint = new Point((int)((140 + threshold) * uintLen_1dB), (int)((140 + threshold + offset) * uintLen_1dB * -1));

            // max point: x axis is fixed to "uintLen_1dB * 164"
            //double 
            maxPoint = new Point((int)(164 * uintLen_1dB), (int)((140 + threshold + offset) * uintLen_1dB * -1));
        }

        /// <summary>
        /// Calcuate Register value from time constant
        /// </summary>
        /// <param name="timeVaule">time constant in mSec</param>
        /// <returns></returns>
        private uint CalcTimeConstant(double timeVaule)
        {
            double ax_d = 1 - Math.Exp((-1 * timeVaule / 1000d) / fs);
            //double ta = -1 * fs / (1 * Math.Log(Math.E, 1 - ax_d));
            return ((uint)(Math.Round(ax_d * Math.Pow(2, 23))) & mask_3p23);
        }

        /// <summary>
        /// Calcuate time constant from Register value 
        /// </summary>
        /// <param name="_bfValue">Register value</param>
        /// <returns></returns>
        private double CalcTimeConstant(uint _bfValue)
        {
            uint _wx = _bfValue & mask_3p23;
            double timeX = -1 * fs / Math.Log(Math.E, _wx / Math.Pow(2, 23));
            return timeX;
        }

        private double CalcThreshold(uint _regValue)
        {
            uint thresh_RegValue = _regValue & 0xFFFFFFFF;
            double thresh_dB = 24 - 6.0206 * thresh_RegValue / Math.Pow(2, 17);
            return thresh_dB;
        }

        private double CalcSlope(uint _regValue)
        {
            uint slope_RegValue = _regValue & mask_3p23;
            double k = slope_RegValue / Math.Pow(2, 23);
            if (k <= 0 && k > -2)
            {
                slope = 1 / (1 + k);
            }
            else
            {
                slope = 1 + k;
            }
            return slope;
        }

        private double CalcOffset(uint _regValue)
        {
            uint offset_RegValue = _regValue & 0xFFFFFFFF;
            double offset_dB = 20 * Math.Log10((offset_RegValue / Math.Pow(2, 17)) * 15.5);
            return offset_dB;
        }

        #endregion Funcs

        private void myPanel_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            #region 画DRC工作区域，点顺序： 上左，上右，下左，下右
            Point p1 = new Point(xAxis_Shift, yAxis_Shift / 2);
            Point p2 = new Point(this.myPanel.Width - xAxis_Shift / 2, yAxis_Shift / 2);
            Point p3 = new Point(xAxis_Shift, this.myPanel.Height - yAxis_Shift);
            Point p4 = new Point(this.myPanel.Width - xAxis_Shift / 2, this.myPanel.Height - yAxis_Shift);
            e.Graphics.DrawLine(pen_Frame, p1, p2);
            e.Graphics.DrawLine(pen_Frame, p3, p4);
            e.Graphics.DrawLine(pen_Frame, p1, p3);
            e.Graphics.DrawLine(pen_Frame, p2, p4);
            #endregion 画DRC工作区域

            #region Comment out
            //Point xy0 = new Point(xAxis_Shift, this.myPanel.Height - yAxis_Shift);
            //Point xAxisEnd = new Point(this.myPanel.Width - xAxis_Shift/2 ,this.myPanel.Height - yAxis_Shift);
            //Point yAxisEnd = new Point(xAxis_Shift, yAxis_Shift);

            //// Draw X, Y Axis
            //e.Graphics.DrawLine(myPen, xy0, xAxisEnd);
            //e.Graphics.DrawLine(myPen, xy0, yAxisEnd);
            
            //// Draw Arrow
            //e.Graphics.FillPolygon(myBrush, new Point[] { xAxisEnd, 
            //    new Point(this.myPanel.Width - xAxis_Shift - arrowSize, this.myPanel.Height - yAxis_Shift - arrowSize / 2),
            //    new Point(this.myPanel.Width - xAxis_Shift - arrowSize, this.myPanel.Height - yAxis_Shift + arrowSize / 2)});

            //e.Graphics.FillPolygon(myBrush, new Point[] { yAxisEnd, 
            //    new Point(xAxis_Shift - arrowSize / 2, yAxis_Shift + arrowSize),
            //    new Point(xAxis_Shift + arrowSize / 2, yAxis_Shift + arrowSize)});

            //// Draw label            
            //e.Graphics.DrawString("Output", this.Font, brush_string, new Point(xAxis_Shift + arrowSize, yAxis_Shift + arrowSize));
            //e.Graphics.DrawString("Input", this.Font, brush_string,
            //    new Point(this.myPanel.Width - 2 * xAxis_Shift - 2 * arrowSize, this.myPanel.Height - yAxis_Shift - arrowSize));
            #endregion Comment out

            #region Draw grids and labels
            pen_smallGrid.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
            // small grids
            for (int ix = 0; ix < smallGrid_L1.Count; ix++)
            {
                e.Graphics.DrawLine(pen_smallGrid, smallGrid_L1[ix], smallGrid_L2[ix]);
                e.Graphics.DrawLine(pen_smallGrid, smallGrid_D1[ix], smallGrid_D2[ix]);
            }

            // larger Grids
            for (int ix = 0; ix < largerGrid_L.Count; ix++)
            {
                e.Graphics.DrawLine(pen_smallGrid, largerGrid_L[ix], largerGrid_R[ix]);
                e.Graphics.DrawLine(pen_smallGrid, largerGrid_U[ix], largerGrid_D[ix]);
            }

            // labels
            for (int ix = 0; ix < labelStr_L.Count; ix++)
            {
                e.Graphics.DrawString(labelStr_L[ix], this.Font, brush_string, label_Loc_L[ix]);
                e.Graphics.DrawString(labelStr_D[ix], this.Font, brush_string, label_Loc_D[ix]);
            }

            #endregion Draw grids and labels


            e.Graphics.TranslateTransform(xAxis_Shift, this.myPanel.Height - yAxis_Shift);
            // Draw Linear line
            int linearLen = (int)((this.Width - 2 * xAxis_Shift) * linearLenRatio);
            e.Graphics.DrawLine(pen_Frame, new Point(0, 0), new Point(linearLen, -linearLen));

            // Draw compression line
            int compressionLen = (int)((this.Width - 2 * xAxis_Shift) * (maxLenRatio - linearLenRatio));
            
            // Draw dash line before compression line because once they are same, dash line will cover real line
            pen_Frame.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
            pen_Frame.Color = Color.LightGray;
            e.Graphics.DrawLine(pen_Frame, new Point(linearLen, -linearLen), 
                new Point(linearLen + (int)(1.5 * compressionLen), -linearLen - (int)(1.5 * compressionLen)));
            e.Graphics.DrawLine(pen_Frame, new Point(linearLen - xAxis_Shift / 2, -linearLen),
                new Point(linearLen + (int)(1.5 * compressionLen), -linearLen));

            if (slope <= 1)
                e.Graphics.DrawLine(pen_Frame, new Point(linearLen + compressionLen, -linearLen - (int)(1.5 * compressionLen)),
                    new Point(linearLen + compressionLen, -linearLen + xAxis_Shift / 2));
            else
                e.Graphics.DrawLine(pen_Frame, new Point(linearLen + (int)(compressionLen / slope), -linearLen - (int)(1.5 * compressionLen)),
                    new Point(linearLen + (int)(compressionLen / slope), -linearLen + xAxis_Shift / 2));
           
            // Draw compression Line
            pen_Frame.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
            //myPen.Color = Color.Gray;

            if (slope <= 1)
                e.Graphics.DrawLine(pen_Frame, new Point(linearLen, -linearLen),
                    new Point(linearLen + compressionLen, -linearLen - 1 * (int)(compressionLen * slope)));
            else
                e.Graphics.DrawLine(pen_Frame, new Point(linearLen, -linearLen),
                    new Point(linearLen + (int)(compressionLen / slope), -linearLen - compressionLen));

            // Test Draw circle 
            pen_Frame.Color = Color.Blue;
            //if(circleCenter == null)
            circleCenter = new Point(linearLen - 5, -linearLen - 5);
            //if(circleRect_1 == null)
            circleRect_1 = new Rectangle(circleCenter, new Size(10, 10));
            //Rectangle circleRect = new Rectangle(new Point(linearLen, linearLen), new Size(10, 10));
            //Point circleCenter = new Point(linearLen, linearLen);
            //int circleRadius = 10;
            e.Graphics.DrawEllipse(pen_Frame, circleRect_1);

            pen_Frame.Color = Color.Gray;
        }

        private void numUP_EnergyTime_ValueChanged(object sender, EventArgs e)
        {
            ae = CalcTimeConstant((double)this.numUP_EnergyTime.Value);
            we = mask_2p23 - ae;
        }

        private void numUP_AttackTime_ValueChanged(object sender, EventArgs e)
        {
            aa = CalcTimeConstant((double)this.numUP_AttackTime.Value);
            wa = mask_2p23 - aa;
        }

        private void numUP_DecayTime_ValueChanged(object sender, EventArgs e)
        {
            ad = CalcTimeConstant((double)this.numUP_DecayTime.Value);
            wd = mask_2p23 - ad;
        }

        private void numUP_Threshold_ValueChanged(object sender, EventArgs e)
        {
            threshold = (double)this.numUP_Threshold.Value;
            double T = (threshold - 24) / -6.0206;
            T_ThresholddB = (uint)T * mask_2p23;
        }

        private void numUP_Slope_ValueChanged(object sender, EventArgs e)
        {
            slope = (double)this.numUP_Slope.Value;
            if(Math.Abs(slope) >= 1)
                K_slopedB = ((uint)(slope - 1) * mask_2p23) & mask_3p23;
            else
                K_slopedB = ((uint)(1/slope - 1) * mask_2p23) * mask_2p23;

            this.myPanel.Refresh();
        }

        private void numUP_Offset_ValueChanged(object sender, EventArgs e)
        {
            offset = (double)this.numUP_Offset.Value;
            double Goffset = (Math.Pow(10, offset / 20d)) / 15.5d;
            O_offsetdB = (uint)(Goffset * mask_2p23) & mask_3p23;
        }
        
        private void chb_Enable_CheckedChanged(object sender, EventArgs e)
        {
            //regList[6][DRC_EN].BFValue
            //regList[6][bf_DRC_EN].BFValue = chb_Enable.Checked ? 1u : 0u;
            DRC_EN = chb_Enable.Checked;
            if (chb_Enable.Checked)
            {
                this.chb_Enable.Text = "Enabled";
                this.chb_Enable.BackColor = Color.GreenYellow;
            }
            else
            {
                this.chb_Enable.Text = "Disabled";
                this.chb_Enable.BackColor = Color.IndianRed;
            }
        }
        
        private void DRCCtrl_SizeChanged(object sender, EventArgs e)
        {

        }

        private void btn_Update_Click(object sender, EventArgs e)
        {
            try
            {
                regList[0]["ae[25:0]"].BFValue = ae;
                regList[0]["(1-ae)[25:0]"].BFValue = we;

                regList[1]["aa[25:0]"].BFValue = aa;
                regList[1]["(1-aa)[25:0]"].BFValue = wa;

                regList[2]["ad[25:0]"].BFValue = ad;
                regList[2]["(1-ad)[25:0]"].BFValue = wd;

                regList[3].RegValue = T_ThresholddB;
                regList[4].RegValue = K_slopedB;
                regList[5].RegValue = O_offsetdB;

                regList[6][bf_DRC_EN].BFValue = DRC_EN ? 1u : 0u;

                myRegOp.RegWrite(regList.ToArray());
                myRegOp.UpdateRegSettingSource();
            }
            catch 
            {
                MessageBox.Show("Update failed!!!");
            }
        }

        Point circleCenter;// = (int)((this.Width - 2 * xAxis_Shift) * linearLenRatio);
        Rectangle circleRect_1;// = new Rectangle(new Point(, linearLen), new Size(10, 10));
        bool canMoveCircle_1 = false;
        private void myPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Console.WriteLine("circle location: {0},{1}", circleCenter.X, circleCenter.Y);
                Console.WriteLine("mouse location: {0},{1}", e.Location.X, e.Location.Y);
                canMoveCircle_1 = true;
                //e.Location 
            }
        }

        private void myPanel_MouseMove(object sender, MouseEventArgs e)
        {
            //Console.WriteLine("Start Moving");
            
            if (canMoveCircle_1)
            {
                Console.WriteLine("Moving: mouse location: {0},{1}", e.Location.X, e.Location.Y);                
            }
        }

        private void myPanel_MouseUp(object sender, MouseEventArgs e)
        {
            canMoveCircle_1 = false;
        }




    }
}
