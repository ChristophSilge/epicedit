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

using System;
using System.ComponentModel;
using System.Windows.Forms;
using EpicEdit.Rom.Settings;

namespace EpicEdit.UI.SettingEdition
{
    internal partial class SuffixedNameControl : UserControl
    {
        [Browsable(true), Category("Behavior")]
        public event EventHandler<EventArgs> SelectedNameChanged;

        [Browsable(true), Category("Behavior")]
        public event EventHandler<EventArgs> SuffixTextChanged;

        private SuffixedTextItem textItem;
        private bool fireEvents;

        public SuffixedNameControl()
        {
            this.InitializeComponent();
        }

        public void Init(SuffixedTextItem textItem)
        {
            this.fireEvents = false;

            this.textItem = textItem;
            this.nameComboBox.Init(Context.Game.Settings.CupAndThemeTexts);
            this.nameComboBox.SelectedItem = this.textItem.TextItem;
            this.suffixTextBox.Text = this.textItem.Suffix.Value;

            this.fireEvents = true;
        }

        private void NameComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.fireEvents && this.SelectedNameChanged != null)
            {
                this.SelectedNameChanged(this, EventArgs.Empty);
            }
        }

        public TextItem SelectedTextItem
        {
            get { return this.nameComboBox.SelectedItem as TextItem; }
        }

        private void SuffixTextBoxTextChanged(object sender, EventArgs e)
        {
            if (!this.fireEvents)
            {
                return;
            }

            this.fireEvents = false;

            int sel = this.suffixTextBox.SelectionStart;
            this.textItem.Suffix.Value = this.suffixTextBox.Text;
            this.suffixTextBox.Text = this.textItem.Suffix.Value; // Retrieve validated text
            this.suffixTextBox.SelectionStart = sel; // Restore text input position

            if (this.SuffixTextChanged != null)
            {
                this.SuffixTextChanged(this, EventArgs.Empty);
            }

            this.fireEvents = true;
        }
    }
}