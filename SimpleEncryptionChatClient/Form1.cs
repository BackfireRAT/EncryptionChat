using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SimpleEncryptionChatClient
{
    public partial class Form1 : Form
    {
        public Socket ServerSocket;
        public Form1()
        {
            InitializeComponent();
        }

        public void PushMessage(String RecvName, String Message, Form1 mainFrm)
        {
            if (mainFrm.richTextBox1.InvokeRequired)
            {
                mainFrm.richTextBox1.BeginInvoke((MethodInvoker)delegate () 
                {
                    mainFrm.richTextBox1.Text = mainFrm.richTextBox1.Text + "[" + RecvName + "] " + Message + Environment.NewLine;
                });
            }
            else
            {
                mainFrm.richTextBox1.Text = mainFrm.richTextBox1.Text + "[" + RecvName + "] " + Message + Environment.NewLine; 
            }
        }

        public void UpdateStatus(String Status, Form1 mainFrm)
        {
            if (mainFrm.InvokeRequired)
            {
                mainFrm.BeginInvoke((MethodInvoker)delegate ()
               {
                   mainFrm.Text = "Simple Encrypted Chat Client - Status : " + Status;
               });
            }
            else
            {
                mainFrm.Text = "Simple Encrypted Chat Client - Status : " + Status;
            }
        }

        public void Connection(String EncPass, String Addr, String Port, Form1 mainFrm)
        {
            UpdateStatus("Connecting...", mainFrm);
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(Addr), Convert.ToInt32(Port));
            ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ServerSocket.Connect(ipEndPoint);
            ServerSocket.Send(Encoding.ASCII.GetBytes(AESEncDec.EncryptString("VariableValue", EncPass)));
            try
            {
                byte[] TempTemp = new byte[4098];
                int TempSizeBuff = ServerSocket.Receive(TempTemp);
                String TempData = Encoding.ASCII.GetString(TempTemp, 0, TempSizeBuff);
                if (TempData == "Success")
                {
                    UpdateStatus("Connected @ " + ServerSocket.RemoteEndPoint.ToString(), mainFrm);
                    while (ServerSocket.Connected)
                    {
                        try
                        {
                            byte[] Buffer = new byte[4098];
                            int SizeBuff = ServerSocket.Receive(Buffer);
                            String EncData = Encoding.ASCII.GetString(Buffer, 0, SizeBuff);
                            String Data = AESEncDec.DecryptString(EncData, EncPass);
                            PushMessage(ServerSocket.RemoteEndPoint.ToString(), Data, mainFrm);
                        }
                        catch
                        {
                            ServerSocket.Disconnect(false);
                            ServerSocket.Close();
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Incorrect password!","Error!");
                }
            }
            catch
            {
                
            }
            UpdateStatus("Idle...", mainFrm);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "Connect")
            {
                try
                {
                    int PortNum = Convert.ToInt32(textBox2.Text);
                    IPAddress ParsedAddr = IPAddress.Parse(textBox1.Text);
                    Thread Running = new Thread((new ThreadStart(() => Connection(textBox3.Text, textBox1.Text, textBox2.Text, this))));
                    Running.Start();
                    textBox3.Enabled = false;
                    textBox1.Enabled = false;
                    textBox2.Enabled = false;
                    button1.Text = "Stop";
                }
                catch
                {
                    MessageBox.Show("The entered IP or port in invalid.", "Error!");
                }
            }
            else if (button1.Text == "Stop")
            {
                try
                {
                    ServerSocket.Disconnect(true);
                }
                catch
                {
                    //Already Disconnected
                }
                textBox3.Enabled = true;
                textBox1.Enabled = true;
                textBox2.Enabled = true;
                button1.Text = "Connect";
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                ServerSocket.Send(Encoding.ASCII.GetBytes(AESEncDec.EncryptString(richTextBox2.Text, textBox3.Text)));
                PushMessage("You", richTextBox2.Text, this);
                richTextBox2.Text = "";
            }
            catch
            {
                MessageBox.Show("Could not send message. Is your socket connected?", "Error!");
            }
        }
    }
}
