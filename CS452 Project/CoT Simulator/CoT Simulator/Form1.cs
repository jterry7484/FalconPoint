using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace CoT_Simulator
{
    public partial class Form1 : Form
    {
        private string file;
        private string current_line;
        private string one_event_line;
        private List<string> event_list;
        private int sleep_length = 1000; //TODO put this in the config file
        private bool loaded_cot_flag = false;
        private int timeOut = 2000;

        /* SAMPLE COTS INPUT
            <event version="2.0" uid="****" how="m-r" time="2012-01-29T00:07:06Z" stale="2012-01-29T00:47:06Z" type="a-f-G" start="2012-01-29T00:07:06Z">
                <detail></detail>
                <point hae="0" lat="34.717305" lon="-86.676109" ce="25" le="10" />
            </event>
         */


        public Form1()
        {
            InitializeComponent();

            event_list = new List<string>();
            get_random_id();

            backgroundWorker1.WorkerSupportsCancellation = true;
        }

        public void Open_File_Click(object sender, EventArgs e)
        {
            DialogResult result = openFileDialog1.ShowDialog();

            if (result == DialogResult.OK)
            {
                file = openFileDialog1.FileName;
            }

            if (file != null)
            {
                loaded_cot_flag = true;

                try
                {

                    using (StreamReader read_line = new StreamReader(file))
                    {
                        while ((current_line = read_line.ReadLine()) != null)
                        {
                            parse_txt();
                        }
                    }
                }
                catch (IOException)
                {
                }

            }

        }

        private void TransmitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            backgroundWorker1.RunWorkerAsync();
        }

        private void parse_txt()
        {
            if (current_line.StartsWith("<?xml") == true)
                combine_line();
            else if (current_line.StartsWith("<event") == true)
            {
                assign_id();
                combine_line();
            }
            else if (current_line.StartsWith("<detail>") == true)
                combine_line();
            else if (current_line.StartsWith("<point") == true)
                combine_line();
            else if (current_line.StartsWith("</event>") == true)
            {
                combine_line();
                add_line_ToList();
            }
            else
                MessageBox.Show("unknown line " + current_line.ToString());
        }

        // Changes stale time to current time, so that we appear to be sending stale data
        private string UpdateStaleTime(string _text)
        {
            string current_lineMod = _text;
            string _time = null;

            if(current_lineMod.Contains("stale=") == true && current_lineMod.Contains("time=") == true); // be sure that we are on a line that has a line called stale & time
            {
                _time = current_lineMod.Remove(0, current_lineMod.IndexOf("time")); // remove everything to the left of time field
                _time = _time.Remove(0, _time.IndexOf("\"")+1); // remove "time=" from string
                _time = _time.Remove(_time.IndexOf("\"")); // remove everything after the quote at the end of time

                int wheresStale = current_lineMod.IndexOf("stale");

                current_lineMod = current_lineMod.Remove(wheresStale + 6, 22); // remove stale time stamp

                _time = "\"" + _time + "\"";

                current_lineMod = current_lineMod.Insert(wheresStale + 6, _time); // insert _time where stale used to be
            }

            return current_lineMod;

        }

        private void combine_line()
        {
            one_event_line = one_event_line + current_line;
        }


        private void add_line_ToList()
        {
            event_list.Add(one_event_line);
            one_event_line = "";
        }


        private void Close_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        public void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            if (rbTCP.Checked)
            {
                TCP();
            }
            else
            {
                UDP();
            }


        } // End backgroundworker1_DoWork

        private void TCP()
        {
            int port = Convert.ToInt32(TB_port.Text);
            IPAddress serverAddr = IPAddress.Parse(TB_IP.Text);

            TcpClient client = new TcpClient();

            IPEndPoint serverEndPoint = new IPEndPoint(serverAddr, port);

            client.SendTimeout = timeOut;
            try
            {
                client.Connect(serverEndPoint);

                NetworkStream clientStream = client.GetStream();

                ASCIIEncoding encoder = new ASCIIEncoding();
                bool restart = CB_loop.Checked;
                do
                {
                  
                    foreach (string text in event_list)
                    {
                        string outputTxt = text;

                        if (staleData.Checked == true) // this means that we need to change the stale data time to equal the current time, so that it appears to be stale
                           outputTxt = UpdateStaleTime(text);

                        byte[] buffer = encoder.GetBytes(outputTxt);
                        clientStream.Write(buffer, 0, buffer.Length);
                        System.Threading.Thread.Sleep(sleep_length);
                    }

                } while (restart == true);
            }
            catch
            {
                MessageBox.Show("Server error... be sure that FalconPoint is running on destination computer.");
            }
        }

        private void UDP()
        {
             try
             {
                 int port = Convert.ToInt32(TB_port.Text);

                 Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                 IPAddress serverAddr = IPAddress.Parse(TB_IP.Text);
                 IPEndPoint endPoint = new IPEndPoint(serverAddr, port);

                 bool restart = CB_loop.Checked;
                 do
                 {
                     foreach (string text in event_list)
                     {
                         byte[] send_buffer = Encoding.ASCII.GetBytes(text);

                         sock.SendTo(send_buffer, endPoint);

                         System.Threading.Thread.Sleep(sleep_length);
                     }
                 } while (restart == true);
             }
             catch (IOException)
             {
             }
        }

        private void random_num_button(object sender, EventArgs e)
        {
            get_random_id();

        }

        private void get_random_id()
        {
            string UID = string.Format("{0:d}", DateTime.Now.Millisecond);
            TB_UID.Text = "G" + UID;
        }

        private void assign_id()
        {
            string UID = TB_UID.Text;
            int wheres_star = current_line.IndexOf("*");
            current_line = current_line.Remove(wheres_star, 4);
            current_line = current_line.Insert(wheres_star, UID);

        }

        private void BUT_StartTransmit_Click(object sender, EventArgs e)
        {
            if (loaded_cot_flag == false)
                MessageBox.Show("Load COTS file first");
            else if (backgroundWorker1.IsBusy == true)
                backgroundWorker1.CancelAsync();
            else
                backgroundWorker1.RunWorkerAsync();
        }






    }
}
