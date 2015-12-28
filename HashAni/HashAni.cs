using SharpShell.Attributes;
using SharpShell.SharpContextMenu;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace HashAni
{
    [ComVisible(true)]
    [COMServerAssociation(AssociationType.AllFiles)]
    public class HashAni : SharpContextMenu
    {
        private static UInt32[] table;
        private const UInt32 seed = 0xffffffffu;
        private const UInt32 poly = 0xedb88320u;
        private byte[] buf;

        protected override bool CanShowMenu()
        {
            return true;
        }

        protected override ContextMenuStrip CreateMenu()
        {
            //initialize menu
            var menu = new ContextMenuStrip();
            //initialize item
            var item = new ToolStripMenuItem
            {
                Text = "Check CRC32 with HashAni"
            };
            item.Click += (sender, args) => checkCRC32();
            //add item to menu
            menu.Items.Add(item);
            //return
            return menu;
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

        private void checkCRC32()
        {
            string message = "";
            bool error = false;
            foreach (var f in SelectedItemPaths)
            {
                FileStream fstr = File.OpenRead(f);
                string hash = ui32ToStr(CRC32(fstr));
                string hashexpr = "[" + hash + "]";
                Regex matcher = new Regex(hash);
                if (!matcher.IsMatch(Path.GetFileName(f).ToUpper()))
                {
                    error = true;
                    message += "ERROR: " + hash + " " + Path.GetFileName(f) + "\n";
                }
                fstr.Close();
            }
            if (error)
            {
                MessageBox.Show(message);
            }
            else
            {
                MessageBox.Show("All Match!");
            }
        }
    }
}