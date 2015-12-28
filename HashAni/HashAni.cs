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
            item.Click += (sender, args) => showForm();
            //add item to menu
            menu.Items.Add(item);
            //return
            return menu;
        }

        private void showForm()
        {
            HashForm form = new HashForm(SelectedItemPaths);
            form.Show();
        }
    }
}