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

using EpicEdit.Rom;
using EpicEdit.Rom.Settings;
using EpicEdit.Rom.Tracks;
using EpicEdit.Rom.Tracks.AI;
using EpicEdit.Rom.Tracks.Items;
using EpicEdit.Rom.Utility;
using EpicEdit.UI.Tools;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace EpicEdit.UI.TrackEdition
{
    /// <summary>
    /// Represents a collection of controls to edit <see cref="TrackAI"/>.
    /// </summary>
    internal partial class AIControl : UserControl
    {
        [Browsable(true), Category("Action")]
        public event EventHandler<EventArgs> AddElementRequested;

        [Browsable(true), Category("Action")]
        public event EventHandler<EventArgs> ItemProbaEditorRequested;

        private bool fireEvents;

        /// <summary>
        /// The current track.
        /// </summary>
        private Track track;

        /// <summary>
        /// The selected AI element.
        /// </summary>
        private TrackAIElement selectedElement;

        /// <summary>
        /// Gets or sets the selected AI element.
        /// </summary>
        [Browsable(false), DefaultValue(typeof(TrackAIElement), "")]
        public TrackAIElement SelectedElement
        {
            get => this.selectedElement;
            set
            {
                this.selectedElement = value;

                if (this.selectedElement == null)
                {
                    this.selectedAIElementGroupBox.Enabled = false;
                }
                else
                {
                    this.selectedAIElementGroupBox.Enabled = true;

                    this.SetMaximumAIElementIndex();

                    this.fireEvents = false;

                    this.indexNumericUpDown.Value = this.track.AI.GetElementIndex(this.selectedElement);
                    this.speedNumericUpDown.Value = this.selectedElement.Speed;
                    this.shapeComboBox.SelectedItem = this.selectedElement.AreaShape;
                    this.isIntersectionCheckBox.Checked = this.selectedElement.IsIntersection;

                    this.fireEvents = true;
                }

                // Force controls to refresh so that the new data shows up instantly.
                // NOTE: We could call this.selectedAIElementGroupBox.Refresh(); instead
                // but that would cause some minor flickering.
                this.indexNumericUpDown.Refresh();
                this.speedNumericUpDown.Refresh();
                this.shapeComboBox.Refresh();
                this.isIntersectionCheckBox.Refresh();
            }
        }

        [Browsable(false), DefaultValue(typeof(Track), "")]
        public Track Track
        {
            get => this.track;
            set
            {
                if (this.track == value)
                {
                    return;
                }

                if (this.track != null)
                {
                    this.track.AI.PropertyChanged -= this.track_AI_PropertyChanged;
                    this.track.AI.ElementAdded -= this.track_AI_ElementAdded;
                    this.track.AI.ElementRemoved -= this.track_AI_ElementRemoved;
                    this.track.AI.ElementsCleared -= this.track_AI_ElementsCleared;
                    
                    if (this.track is GPTrack oldGPTrack)
                    {
                        oldGPTrack.PropertyChanged -= this.gpTrack_PropertyChanged;
                    }
                }

                this.track = value;

                this.track.AI.PropertyChanged += this.track_AI_PropertyChanged;
                this.track.AI.ElementAdded += this.track_AI_ElementAdded;
                this.track.AI.ElementRemoved += this.track_AI_ElementRemoved;
                this.track.AI.ElementsCleared += this.track_AI_ElementsCleared;

                if (this.track is GPTrack gpTrack)
                {
                    gpTrack.PropertyChanged += this.gpTrack_PropertyChanged;
                }

                this.SelectedElement = null;
                this.LoadItemProbabilitySet();
                this.SetMaximumAIElementIndex();
                this.warningLabel.Visible = this.track.AI.ElementCount == 0;
            }
        }

        public AIControl()
        {
            this.InitializeComponent();
            this.Init();
        }

        private void Init()
        {
            this.InitSetComboBox();
            this.shapeComboBox.DataSource = Enum.GetValues(typeof(TrackAIElementShape));
        }

        private void InitSetComboBox()
        {
            this.AddSetComboBoxItems();
            this.setComboBox.SelectedIndex = 0;
        }

        private void AddSetComboBoxItems()
        {
            this.setComboBox.BeginUpdate();

            for (int i = 0; i < ItemProbabilities.SetCount; i++)
            {
                this.setComboBox.Items.Add("Probability set " + (i + 1));
            }

            this.setComboBox.EndUpdate();
        }

        private void ResetSetComboBoxGP()
        {
            this.setComboBox.Items.Clear();
            this.AddSetComboBoxItems();
            this.setComboBox.Enabled = true;
        }

        private void ResetSetComboBoxBattle()
        {
            this.setComboBox.Items.Clear();
            TextCollection modeNames = Context.Game.Settings.ModeNames;
            this.setComboBox.Items.Add(modeNames[modeNames.Count - 1].FormattedValue);
            this.setComboBox.SelectedIndex = 0;
            this.setComboBox.Enabled = false;
        }

        private void SetComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            if (!this.fireEvents)
            {
                return;
            }

            GPTrack gpTrack = this.track as GPTrack;
            gpTrack.ItemProbabilityIndex = this.setComboBox.SelectedIndex;
        }

        private void ShapeComboBoxFormat(object sender, ListControlConvertEventArgs e)
        {
            e.Value = UITools.GetDescription(e.Value);
        }

        private void IndexNumericUpDownValueChanged(object sender, EventArgs e)
        {
            if (!this.fireEvents)
            {
                return;
            }

            int oldIndex = this.track.AI.GetElementIndex(this.selectedElement);
            int newIndex = (int)this.indexNumericUpDown.Value;
            this.track.AI.ChangeElementIndex(oldIndex, newIndex);
        }

        private void SpeedNumericUpDownValueChanged(object sender, EventArgs e)
        {
            if (!this.fireEvents)
            {
                return;
            }

            this.selectedElement.Speed = (byte)this.speedNumericUpDown.Value;
        }

        private void ShapeComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            if (!this.fireEvents)
            {
                return;
            }

            this.selectedElement.AreaShape = (TrackAIElementShape)this.shapeComboBox.SelectedValue;
        }

        private void IsIntersectionCheckBoxCheckedChanged(object sender, EventArgs e)
        {
            if (!this.fireEvents)
            {
                return;
            }

            this.selectedElement.IsIntersection = this.isIntersectionCheckBox.Checked;
        }

        private void CloneButtonClick(object sender, EventArgs e)
        {
            TrackAIElement aiElement = this.SelectedElement;
            TrackAIElement newAIElem = aiElement.Clone();

            // Shift the cloned element position, so it's not directly over the source element
            newAIElem.Location = new Point(aiElement.Location.X + TrackAIElement.Precision,
                                           aiElement.Location.Y + TrackAIElement.Precision);

            // Ensure the cloned element index is right after the source element
            int newAIElementIndex = this.track.AI.GetElementIndex(aiElement) + 1;

            this.track.AI.Insert(newAIElem, newAIElementIndex);
        }

        private void DeleteButtonClick(object sender, EventArgs e)
        {
            this.track.AI.Remove(this.SelectedElement);
        }

        private void LoadItemProbabilitySet()
        {
            this.fireEvents = false;

            if (this.track is GPTrack gpTrack)
            {
                if (!this.setComboBox.Enabled)
                {
                    this.ResetSetComboBoxGP();
                }

                this.setComboBox.SelectedIndex = gpTrack.ItemProbabilityIndex;
            }
            else
            {
                if (this.setComboBox.Enabled)
                {
                    this.ResetSetComboBoxBattle();
                }
            }

            this.fireEvents = true;
        }

        private void ProbaEditorButtonClick(object sender, EventArgs e)
        {
            this.ItemProbaEditorRequested(this, EventArgs.Empty);
        }

        private void SetMaximumAIElementIndex()
        {
            this.fireEvents = false;
            this.indexNumericUpDown.Maximum = this.track.AI.ElementCount - 1;
            this.fireEvents = true;
        }

        private void AddButtonClick(object sender, EventArgs e)
        {
            this.AddElementRequested(this, EventArgs.Empty);
        }

        private void DeleteAllButtonClick(object sender, EventArgs e)
        {
            DialogResult result = UITools.ShowWarning("Do you really want to delete all AI elements?");

            if (result == DialogResult.Yes)
            {
                this.track.AI.Clear();
            }
        }

        private void track_AI_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            TrackAIElement aiElement = sender as TrackAIElement;

            if (this.SelectedElement != aiElement)
            {
                return;
            }

            switch (e.PropertyName)
            {
                case PropertyNames.TrackAIElement.Index:
                    this.indexNumericUpDown.Value = this.track.AI.GetElementIndex(this.selectedElement);
                    break;

                case PropertyNames.TrackAIElement.Speed:
                    this.speedNumericUpDown.Value = aiElement.Speed;
                    break;

                case PropertyNames.TrackAIElement.AreaShape:
                    this.shapeComboBox.SelectedItem = aiElement.AreaShape;
                    break;

                case PropertyNames.TrackAIElement.IsIntersection:
                    this.isIntersectionCheckBox.Checked = aiElement.IsIntersection;
                    break;
            }
        }

        private void gpTrack_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == PropertyNames.GPTrack.ItemProbabilityIndex)
            {
                this.setComboBox.SelectedIndex = (this.track as GPTrack).ItemProbabilityIndex;
            }
        }

        private void track_AI_ElementAdded(object sender, EventArgs<TrackAIElement> e)
        {
            this.SetMaximumAIElementIndex();

            if (this.track.AI.ElementCount > 0)
            {
                this.HideWarning();
            }
        }

        private void track_AI_ElementRemoved(object sender, EventArgs<TrackAIElement> e)
        {
            this.SetMaximumAIElementIndex();

            if (this.track.AI.ElementCount == 0)
            {
                this.ShowWarning();
            }
        }

        private void track_AI_ElementsCleared(object sender, EventArgs e)
        {
            this.ShowWarning();
        }

        private void ShowWarning()
        {
            this.warningLabel.Visible = true;
        }

        private void HideWarning()
        {
            this.warningLabel.Visible = false;
        }
    }
}
