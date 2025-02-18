using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace Relocation_Section_Editor
{
    public partial class frmMain : Form
    {
        private Relocations rel = null;
        private int pageIndex = 0;
        private uint baseAddress = 0;
        private string argPath = "";

        public frmMain(string[] args)
        {
            InitializeComponent();

            if (args.Length > 0)
                argPath = args[0];
        }

        private void mnuMainFileExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void mnuMainHelpAbout_Click(object sender, EventArgs e)
        {
            string msg = "This program has been coded by gta126, with minor changes by MHLoppy.";
            string caption = "About";
            MessageBox.Show(msg, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void mnuMainFileOpen_Click(object sender, EventArgs e)
        {
            string path = "";

            if (!string.IsNullOrEmpty(argPath))
            {
                path = argPath;
                argPath = "";
            }
            else
            {
                dlgOpen.Title = "Select an executable file";
                dlgOpen.Filter = "Executable (*.exe)|*.exe" +
                      "|Dynamic Link Library (*.dll)|*.dll" +
                      "|Drivers (*.sys)|*.sys" +
                      "|Windows Visual Style (*.msstyles)|*.msstyles" +
                      "|Configuration Panel Widget (*.cpl)|*.cpl" +
                      "|ActiveX Library (*.ocx)|*.ocx" +
                      "|ActiveX Cache Library (*.oca)|*.oca" +
                      "|Multi User Interface (*.mui)|*.mui" +
                      "|Codecs (*.acm, *.ax)|*.acm;*.ax" +
                      "|Borland / Delphi Library (*.bpl, *.dpl)|*.bpl;*.dpl" +
                      "|Screensaver (*.scr)|*.scr" +
                      "|All Executables (*.exe, *.dll, *.sys, *.msstyles, *.cpl, *.ocx, *.oca, *.mui, *.acm, *.ax, *.bpl, *.dpl, *.scr)|*.exe;*.dll;*.sys;*.msstyles;*.cpl;*.ocx;*.oca;*.mui;*.acm;*.ax;*.bpl;*.dpl;*.scr" +
                      "|All Files (*.*)|*.*";

                if (dlgOpen.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;

                path = dlgOpen.FileName;
            }

            try
            {
                rel = new Relocations(path);

                this.Text = "Relocation Section Editor - " + rel.GetPath();

                RefreshData();

                cmnuPages.Enabled = true;
                cmnuRelocations.Enabled = true;
                mnuMainFileSaveAs.Enabled = true;
                mnuMainFileSave.Enabled = true;
            }
            catch (FileNotFoundException)
            {
                string msg = "File not found.";
                string caption = "Error";
                MessageBox.Show(msg, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (InvalidOperationException ex)
            {
                string msg = "";
                string caption = "Error";

                switch (ex.Message)
                {
                    case "MZ":
                        msg = "MZ Header not found.";
                        break;
                    case "PE":
                        msg = "PE Header not found.";
                        break;
                    case "X86":
                        msg = "Is not a 32bits executable.";
                        break;
                    case "RAW":
                        msg = "No relocation table in this file.";
                        break;
                    default:
                        msg = "Unknown error.";
                        break;
                }

                MessageBox.Show(msg, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void lvPage_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvPage.SelectedItems.Count < 1)
                return;

            lvRelocation.Items.Clear();

            uint address = (uint)lvPage.SelectedItems[0].Tag;
            baseAddress = address;
            pageIndex = lvPage.SelectedItems[0].Index;

            List<Relocations.Reloc> relocs;
            if (!rel.TryGetRelocs(address, out relocs))
                return;

            foreach (Relocations.Reloc reloc in relocs)
            {
                ListViewItem item;

                if (reloc.type == Relocations.BASE_RELOCATION_TYPE.ABSOLUTE)
                    item = new ListViewItem("0x" + reloc.offset.ToString("X8"));
                else
                    item = new ListViewItem("0x" + (address + reloc.offset).ToString("X8"));
                item.SubItems.Add(reloc.type.ToString());
                item.Tag = reloc;

                lvRelocation.Items.Add(item);
            }
        }

        private void cmuRelocationsDelete_Click(object sender, EventArgs e)
        {
            if (lvRelocation.SelectedIndices.Count < 1)
                return;

            Relocations.Reloc reloc = (Relocations.Reloc)lvRelocation.SelectedItems[0].Tag;
            string msg = "Are you sure to delete the relocation address \"" + (baseAddress + reloc.offset) + "\"?";
            string caption = "Confirmation";
            if (MessageBox.Show(msg, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.No)
                return;

            rel.DeleteRelocation(baseAddress + reloc.offset);

            RefreshData();
        }

        private void RefreshData()
        {
            int index = lvPage.SelectedIndices.Count < 1 ? 0 : lvPage.SelectedIndices[0];
            lvPage.Items.Clear();
            lvRelocation.Items.Clear();

            foreach (Relocations.Page page in rel.GetPages())
            {
                ListViewItem item = new ListViewItem("0x" + page.address.ToString("X8"));
                item.SubItems.Add("0x" + page.size.ToString("X8"));
                item.SubItems.Add(page.count.ToString());
                item.Tag = page.address;

                lvPage.Items.Add(item);
            }

            if (index < lvPage.Items.Count)
                lvPage.Items[index].Selected = true;
            else
                lvPage.Items[0].Selected = true;

            RefreshSize();
        }

        private void RefreshSize()
        {
            staLblCurrentSize.Text = "Current size: 0x" + rel.GetVirtualSize().ToString("X8");
            staLblMaxSize.Text = "Max size: 0x" + rel.GetRawSize().ToString("X8");

            int min = 0;
            int max = (int)rel.GetRawSize();
            int value = (int)rel.GetVirtualSize();
            int remaining = max - value;

            staPbSize.Minimum = min;
            staPbSize.Maximum = max;
            staPbSize.Value = value;

            staPbSizeLabel.Text = "(" + remaining + " bytes left)";
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            cmnuPages.Enabled = false;
            cmnuRelocations.Enabled = false;
            mnuMainFileSaveAs.Enabled = false;
            mnuMainFileSave.Enabled = false;

            if (!string.IsNullOrEmpty(argPath))
                mnuMainFileOpen_Click(null, null);
        }

        private void cmnuPagesDelete_Click(object sender, EventArgs e)
        {
            if (lvPage.SelectedItems.Count < 1)
                return;

            string msg = "Are you sure to delete this page?";
            string caption = "Confirmation";
            if (MessageBox.Show(msg, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.No)
                return;

            foreach (ListViewItem item in lvRelocation.Items)
            {
                Relocations.Reloc reloc = (Relocations.Reloc)item.Tag;
                rel.DeleteRelocation(baseAddress + reloc.offset);
            }

            lvRelocation.Items.Clear();
            lvPage.Items[lvPage.SelectedIndices[0]].Remove();

            RefreshSize();
        }

        private void mnuAdd_Click(object sender, EventArgs e)
        {
            frmAddRelocation frm = new frmAddRelocation();

            if (frm.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                return;

            int code = rel.AddRelocation(frm.GetAddress(), frm.GetRelocType());
            string msg = "";
            string caption = "Error";

            switch (code)
            {
                case -1:
                    msg = "This address is already in the relocation table.";
                    MessageBox.Show(msg, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
                case 0:
                    msg = "Cannot add this address (0x" + frm.GetAddress().ToString("X8") + ").";
                    MessageBox.Show(msg, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
                case 1:
                case 2:
                    RefreshData();
                    break;
                default:
                    msg = "Unknown error.";
                    MessageBox.Show(msg, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
            }
        }

        private void mnuRelocationsEdit_Click(object sender, EventArgs e)
        {
            if (lvRelocation.SelectedItems.Count < 1)
                return;

            Relocations.Reloc reloc = (Relocations.Reloc)lvRelocation.SelectedItems[0].Tag;
            frmEditRelocation frm = new frmEditRelocation(baseAddress + reloc.offset, reloc.type);

            if (frm.ShowDialog() != DialogResult.OK)
                return;

            if (!rel.EditRelocation(frm.GetOldAddress(), frm.GetNewAddress(), frm.GetRelocType()))
            {
                string msg = "Cannot edit this address.";
                string caption = "Error";
                MessageBox.Show(msg, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            RefreshData();
        }

        private void mnuMainFileSave_Click(object sender, EventArgs e)
        {
            if (!rel.WriteRelocations())
            {
                string msg = "File not saved.";
                string caption = "Error";
                MessageBox.Show(msg, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (rel != null && rel.IsNotSaved)
            {
                string msg = "Are you sure you want to exit the editor without saving the changes that have been made?";
                string caption = "Unsaved Changes";
                if (MessageBox.Show(msg, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2) == DialogResult.No)
                    e.Cancel = true;
            }
        }

        private void mnuMainFileSaveAs_Click(object sender, EventArgs e)
        {
            dlgSave.Filter = "Executable (*.exe)|*.exe" +
                      "|Dynamic Link Library (*.dll)|*.dll" +
                      "|Drivers (*.sys)|*.sys" +
                      "|Windows Visual Style (*.msstyles)|*.msstyles" +
                      "|Configuration Panel Widget (*.cpl)|*.cpl" +
                      "|ActiveX Library (*.ocx)|*.ocx" +
                      "|ActiveX Cache Library (*.oca)|*.oca" +
                      "|Multi User Interface (*.mui)|*.mui" +
                      "|Codecs (*.acm, *.ax)|*.acm;*.ax" +
                      "|Borland / Delphi Library (*.bpl, *.dpl)|*.bpl;*.dpl" +
                      "|Screensaver (*.scr)|*.scr" +
                      "|All Executables (*.exe, *.dll, *.sys, *.msstyles, *.cpl, *.ocx, *.oca, *.mui, *.acm, *.ax, *.bpl, *.dpl, *.scr)|*.exe;*.dll;*.sys;*.msstyles;*.cpl;*.ocx;*.oca;*.mui;*.acm;*.ax;*.bpl;*.dpl;*.scr" +
                      "|All Files (*.*)|*.*";

            switch (Path.GetExtension(rel.GetPath()))
            {
                case ".exe":
                    dlgSave.FilterIndex = 1;
                    break;
                case ".dll":
                    dlgSave.FilterIndex = 2;
                    break;
                case ".sys":
                    dlgSave.FilterIndex = 3;
                    break;
                case ".msstyles":
                    dlgSave.FilterIndex = 4;
                    break;
                case ".cpl":
                    dlgSave.FilterIndex = 5;
                    break;
                case ".ocx":
                    dlgSave.FilterIndex = 6;
                    break;
                case ".oca":
                    dlgSave.FilterIndex = 7;
                    break;
                case ".mui":
                    dlgSave.FilterIndex = 8;
                    break;
                case ".acm":
                case ".ax":
                    dlgSave.FilterIndex = 9;
                    break;
                case ".bpl":
                case ".dpl":
                    dlgSave.FilterIndex = 10;
                    break;
                case ".scr":
                    dlgSave.FilterIndex = 11;
                    break;
            }
            

            if (dlgSave.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                return;

            if (!rel.WriteRelocations(dlgSave.FileName))
            {
                string msg = "Failed to write the relocation section into the file.";
                string caption = "File Not Saved";
                MessageBox.Show(msg, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                string msg = "Relocation section successfully written into the file.";
                string caption = "File Saved";
                MessageBox.Show(msg, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.Text = "Relocation Section Editor - " + rel.GetPath();
            }
        }
    }
}
