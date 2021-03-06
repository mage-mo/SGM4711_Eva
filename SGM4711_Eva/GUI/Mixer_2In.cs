﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MD.MDCommon;

namespace SGM4711_Eva.GUI
{
    public partial class Mixer_2In : Form
    {
        IRegOperation myRegOp;
        string[] inputBF = new string[2];
        Register reg;

        public Mixer_2In(Register _reg, string[] _inputBF, IRegOperation _myRegOp)
        {
            InitializeComponent();

            reg = _reg;
            this.inputBF = _inputBF;
            this.myRegOp = _myRegOp;

            InitGUI();
        }

        private void InitGUI()
        {
            this.numUP_Input0.ValueChanged -= numUP_Input0_ValueChanged;
            this.numUP_Input1.ValueChanged -= numUP_Input1_ValueChanged;

            if (reg.Contain(inputBF[0]))
                this.numUP_Input0.Value = (decimal)((double)reg[inputBF[0]].BFValue / Math.Pow(2, 23));

            if (reg.Contain(inputBF[1]))
                this.numUP_Input1.Value = (decimal)((double)reg[inputBF[1]].BFValue / Math.Pow(2, 23));

            this.numUP_Input0.ValueChanged += numUP_Input0_ValueChanged;
            this.numUP_Input1.ValueChanged += numUP_Input1_ValueChanged;
        }

        private void numUP_Input0_ValueChanged(object sender, EventArgs e)
        {
            reg[inputBF[0]].BFValue = (uint)Math.Round((double)this.numUP_Input0.Value * Math.Pow(2, 23));

            myRegOp.RegWrite(reg);
            myRegOp.UpdateRegSettingSource();
        }

        private void numUP_Input1_ValueChanged(object sender, EventArgs e)
        {
            reg[inputBF[1]].BFValue = (uint)Math.Round((double)this.numUP_Input1.Value * Math.Pow(2, 23));

            myRegOp.RegWrite(reg);
            myRegOp.UpdateRegSettingSource();
        }

    }
}
