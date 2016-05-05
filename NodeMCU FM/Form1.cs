using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;


namespace NodeMCU_FM
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if (!SerialPort.GetInstance().CurrentSp.IsOpen)
            {
                SerialPortComboBox.ComboBox.DataSource = SerialPort.GetPortNames();
            }
            else
            {
                string line=SerialPort.GetInstance().Execute("for k, v in pairs(file.list()) do print(k) end");
                resultbox.AppendText(line);
            }
            
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            if (!SerialPort.GetInstance().CurrentSp.IsOpen)
            {
                if (SerialPort.GetInstance().Open((string)SerialPortComboBox.SelectedItem, Convert.ToInt32(BautrateComboBox.SelectedItem)))
                {
                    connectButton.Image = global::NodeMCU_FM.Properties.Resources.connected;
                    SerialPortComboBox.Enabled = false;
                    BautrateComboBox.Enabled = false;
                }
            }
            else
            {
                connectButton.Image = global::NodeMCU_FM.Properties.Resources.disconnected;
                SerialPort.GetInstance().Close();
                SerialPortComboBox.Enabled = true;
                BautrateComboBox.Enabled = true;
            }
        }

    }
}
