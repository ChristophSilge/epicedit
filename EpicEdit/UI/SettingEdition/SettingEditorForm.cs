﻿#region GPL statement
/*Epic Edit is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, version 3 of the License.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.*/
#endregion

using EpicEdit.Properties;
using EpicEdit.Rom;
using EpicEdit.Rom.Tracks;
using EpicEdit.Rom.Utility;
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace EpicEdit.UI.SettingEdition
{
    internal partial class SettingEditorForm : Form
    {
        /// <summary>
        /// Raised when a pixel color has been selected.
        /// </summary>
        [Browsable(true), Category("Behavior")]
        public event EventHandler<EventArgs<Palette, int>> ColorSelected
        {
            add => this.itemProbaEditor.ColorSelected += value;
            remove => this.itemProbaEditor.ColorSelected -= value;
        }

        public SettingEditorForm()
        {
            this.InitializeComponent();
            this.tabImageList.Images.Add("ItemProbaButton", Resources.ItemProbaButton);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }

        public Theme Theme
        {
            get => this.itemProbaEditor.Theme;
            set => this.itemProbaEditor.Theme = value;
        }

        public void Init()
        {
            this.gpCupTextsEditor.Init();
            this.courseSelectTextsEditor.Init();
            this.courseSelectNamesEditor.Init();
            this.resultsEditor.Init();
            this.itemProbaEditor.Init();
        }

        public void ShowTrackItemProbabilities(Track track, bool showItemProba)
        {
            this.itemProbaEditor.ShowTrackData(track);

            if (showItemProba)
            {
                this.tabControl.SelectedTab = this.itemProbaTab;
            }
        }
    }
}
