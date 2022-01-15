using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FieryGunHand
{
    public partial class OpenMapForm : Form
    {
        private List<WadArchive.Map> mapList = new List<WadArchive.Map>();

        private WadArchive archive;

        public WadArchive.Map? SelectedMap { get; private set; } = null;

        public OpenMapForm(WadArchive archive)
        {
            this.archive = archive;

            InitializeComponent();
        }

        private void OpenMapForm_Load(object sender, EventArgs e)
        {
            PopulateListBox();
        }

        private void PopulateListBox()
        {
            mapList.Clear();
            listBox.Items.Clear();
            foreach (var map in archive.Maps)
            {
                mapList.Add(map);
                listBox.Items.Add($"{map.Name} ({map.Format}/{map.NodesFormat})");
            }
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void buttonOpen_Click(object sender, EventArgs e)
        {
            SelectedMap = mapList[listBox.SelectedIndex];
            DialogResult = DialogResult.OK;
            Close();
        }

        private void listBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool selected = listBox.SelectedIndices.Count == 1;

            buttonOpen.Enabled = selected;
        }
    }
}
