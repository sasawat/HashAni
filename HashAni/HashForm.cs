using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace HashAni
{
    public partial class HashForm : Form
    {
        private static UInt32[] table;
        private const UInt32 seed = 0xffffffffu;
        private const UInt32 poly = 0xedb88320u;
        private byte[] buf;
        private IEnumerable<string> itemsToCheck;
        private IEnumerator<string> next;
        private bool error;
        private string errmsg;
        private BackgroundWorker bw;

        public HashForm(IEnumerable<string> items)
        {
            InitializeComponent();
            itemsToCheck = items;
            error = false;
            errmsg = "";
            next = itemsToCheck.GetEnumerator();
            bw = new BackgroundWorker();
            bw.DoWork += new DoWorkEventHandler(bw_CRC32);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_Done);
        }

        private void println(string str)
        {
            textOut.AppendText(str + "\r\n");
        }

        private void makeTable()
        {
            //we already have table...
            if (table != null) return;

            //init table
            table = new UInt32[256];

            //loop through calculating entries
            for (UInt32 i = 0; i < 256; ++i)
            {
                UInt32 entry = i;
                for (int j = 0; j < 8; ++j)
                {
                    if ((entry & 1) == 1) entry = (entry >> 1) ^ poly;
                    else entry = entry >> 1;
                }
                table[i] = entry;
            }
        }

        private UInt32 CRC32(FileStream fstr)
        {
            //byte table driven CRC32
            //avoid slow ReadByte calls to the filestream with 128MB internal buffer
            if (table == null) makeTable();
            if (this.buf == null) this.buf = new byte[134217728];
            UInt32 hash = seed;
            while (fstr.Position < fstr.Length)
            {
                int rdsz = (int)(134217728 > fstr.Length - fstr.Position ? fstr.Length - fstr.Position : 134217728);
                fstr.Read(buf, 0, rdsz);
                for (int i = 0; i < rdsz; ++i)
                {
                    hash = (hash >> 8) ^ table[buf[i] ^ hash & 0xff];
                }
            }
            return ~hash;
        }

        private string ui32ToStr(UInt32 ui32)
        {
            string ret = "";

            byte[] a = BitConverter.GetBytes(ui32);
            if (BitConverter.IsLittleEndian) Array.Reverse(a);

            foreach (byte x in a)
            {
                ret += x.ToString("x2").ToUpper();
            }

            return ret;
        }

        private void bw_CRC32(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            string f = e.Argument as string;
            //open file
            FileStream fstr = File.OpenRead(f);
            //get hash
            string hash = ui32ToStr(CRC32(fstr));
            //check hash
            Regex matcher = new Regex(hash);
            if (!matcher.IsMatch(Path.GetFileName(f).ToUpper()))
            {
                e.Result = new Tuple<string, string>(Path.GetFileName(f), "Error! Real Hash: " + hash);
            }
            else
            {
                e.Result = new Tuple<string, string>(Path.GetFileName(f), "Match!");
            }
            //close file
            fstr.Close();
        }

        private void bw_Done(object sender, RunWorkerCompletedEventArgs e)
        {
            Tuple<string, string> res = e.Result as Tuple<string, string>;
            //Print the result message
            println(res.Item2);
            //Determine if error
            if (res.Item2[0] == 'E')
            {
                error = true;
                errmsg += res.Item2 + " File: " + res.Item1 + "\n";
            }

            //Process next item or finish
            processNext();
        }

        private void processNext()
        {
            //if we are not done then do
            if (next.MoveNext())
            {
                //get next thing to process
                string f = next.Current;
                //tell the user what we are working on
                println("Processing " + f);
                //run the background worker
                bw.RunWorkerAsync(f);
            }
            else //we have finished processing all items
            {
                if (error)
                {
                    MessageBox.Show(errmsg);
                    btnOk.Enabled = true;
                    btnOk.Select();
                }
                else
                {
                    MessageBox.Show("All Match!");
                    this.Close();
                }
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void HashForm_Shown(object sender, EventArgs e)
        {
            textOut.Text = "HashAni Anime Hash Check Utility\r\n";
            processNext();
        }
    }
}