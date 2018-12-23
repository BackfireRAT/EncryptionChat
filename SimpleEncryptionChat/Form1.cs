using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Threading;

namespace SimpleEncryptionChat
{
    public partial class Form1 : Form
    {
        public Socket ClientSocket;
        public bool ListeningStatus = false;
        public Form1()
        {
            InitializeComponent();
        }

        //To Do
        //Fix Multi Bind
        //Follow End Line
        //Fix Wrong Password
        //Account For DNS Entries
        //Kill Running Thread

        public void PushMessage(String SentFrom, String Message, Form1 mainFrm)
        {
            if (mainFrm.richTextBox1.InvokeRequired)
            {
                mainFrm.richTextBox1.BeginInvoke((MethodInvoker)delegate () 
                {
                    mainFrm.richTextBox1.Text = mainFrm.richTextBox1.Text + "[" + SentFrom + "] " + Message + Environment.NewLine;
                });
            }
            else
            {
                mainFrm.richTextBox1.Text = mainFrm.richTextBox1.Text + "[" + SentFrom + "] " + Message + Environment.NewLine;
            }
        }

        public void UpdateStatus(String Status, Form1 mainFrm)
        {
            if (mainFrm.InvokeRequired)
            {
                mainFrm.BeginInvoke((MethodInvoker)delegate ()
               {
                   mainFrm.Text = "Simple Encrypted Chat - Status : " + Status;
               });
            }
            else
            {
                mainFrm.Text = "Simple Encrypted Chat - Status : " + Status;
            }
        }

        public void Listener(String EncPass, String Port, Form1 mainFrm)
        {
            while (ListeningStatus)
            {
                UpdateStatus("Listening on " + Port + "...", mainFrm);
                LingerOption death = new LingerOption(false, 0);
                IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, Convert.ToInt32(Port));
                Socket ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //MessageBox.Show("Pause");
                ServerSocket.Bind(localEndPoint); //Fix Multiple Binds
                ServerSocket.Listen(10);
                ServerSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, death);
                ClientSocket = ServerSocket.Accept();
                byte[] TempRecv = new byte[4098];
                int TempSize = ClientSocket.Receive(TempRecv);
                String TempEnc = Encoding.ASCII.GetString(TempRecv, 0, TempSize);
                try
                {
                    String Response = AESEncDec.DecryptString(TempEnc, EncPass);
                    if (Response == "VariableValue")
                    {
                        ClientSocket.Send(Encoding.ASCII.GetBytes("Success"));
                        UpdateStatus("Connected @ " + ClientSocket.RemoteEndPoint.ToString(), mainFrm);
                        while (ClientSocket.Connected)
                        {
                            try
                            {
                                byte[] Buffer = new byte[4098];
                                int SizeAccept = ClientSocket.Receive(Buffer);
                                String EncData = Encoding.ASCII.GetString(Buffer, 0, SizeAccept);
                                String Data = AESEncDec.DecryptString(EncData, EncPass);
                                PushMessage(ClientSocket.RemoteEndPoint.ToString(), Data, mainFrm);
                            }
                            catch
                            {
                                try
                                {
                                    ServerSocket.Disconnect(false);
                                    ServerSocket.Close();
                                    ServerSocket.Dispose();
                                    ClientSocket.Disconnect(false);
                                    ClientSocket.Close();
                                    ClientSocket.Dispose();
                                }
                                catch(Exception ex) { MessageBox.Show(ex.ToString(), "Fail 1"); }
                            }
                        }
                    }
                    else
                    {
                        ClientSocket.Send(Encoding.ASCII.GetBytes("Fail"));
                        try
                        {
                            ServerSocket.Disconnect(false);
                            ServerSocket.Close();
                            ServerSocket.Dispose();
                            ClientSocket.Disconnect(false);
                            ClientSocket.Close();
                            ClientSocket.Dispose();
                        }
                        catch(Exception ex) { MessageBox.Show(ex.ToString(), "Fail 2"); }
                    }
                }
                catch
                {
                    ClientSocket.Send(Encoding.ASCII.GetBytes("Fail"));
                    try
                    {
                        ServerSocket.Disconnect(false);
                        ServerSocket.Close();
                        ServerSocket.Dispose();
                        ClientSocket.Disconnect(false);
                        ClientSocket.Close();
                        ClientSocket.Dispose();
                    }
                    catch(Exception ex) { MessageBox.Show(ex.ToString(), "Fail 3"); }
                }  
            }
            UpdateStatus("Idle...", mainFrm);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "Listen")
            {
                try
                {
                    int PortNum = Convert.ToInt32(textBox1.Text);
                    ListeningStatus = true;
                    Thread Running = new Thread((new ThreadStart(() => Listener(textBox2.Text, textBox1.Text, this))));
                    Running.Start();
                    textBox1.Enabled = false;
                    textBox2.Enabled = false;
                    button1.Text = "Stop";
                }
                catch
                {
                    MessageBox.Show("The entered port is not valid.","Error!");
                }
            }
            else if (button1.Text == "Stop")
            {
                ListeningStatus = false;
                try
                {
                    ClientSocket.Disconnect(false);
                    ClientSocket.Close();
                    ClientSocket.Send(Encoding.ASCII.GetBytes(AESEncDec.EncryptString("*Disconnected*", textBox2.Text)));
                }
                catch
                {

                }
                textBox1.Enabled = true;
                textBox2.Enabled = true;
                button1.Text = "Listen";
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                ClientSocket.Send(Encoding.ASCII.GetBytes(AESEncDec.EncryptString(richTextBox2.Text, textBox2.Text)));
                PushMessage("You", richTextBox2.Text, this);
                richTextBox2.Text = "";
            }
            catch
            {
                MessageBox.Show("Could not send message. Is your socket connected?","Error!");
            }
        }
    }
}
