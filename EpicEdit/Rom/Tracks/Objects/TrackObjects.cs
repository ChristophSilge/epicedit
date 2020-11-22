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

using EpicEdit.Rom.Tracks.AI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace EpicEdit.Rom.Tracks.Objects
{
    /// <summary>
    /// A collection of 16 <see cref="TrackObject"/> objects, and 6 <see cref="TrackObjectMatchRace"/> objects.
    /// </summary>
    internal class TrackObjects : IEnumerable<TrackObject>, INotifyPropertyChanged
    {
        public const int RegularObjectCount = 16;
        public const int MatchRaceObjectCount = 6;
        public const int ObjectCount = RegularObjectCount + MatchRaceObjectCount;
        private const int BytesPerObject = 2;
        public const int Size = ObjectCount * BytesPerObject;

        public event PropertyChangedEventHandler PropertyChanged;

        private readonly TrackObject[] objects;

        public TrackObjectAreas Areas { get; }

        public TrackObjectProperties Properties { get; }

        public TrackObjectType Tileset
        {
            get => this.Properties.Tileset;
            set => this.Properties.Tileset = value;
        }

        public TrackObjectType Interaction
        {
            get => this.Properties.Interaction;
            set => this.Properties.Interaction = value;
        }

        public TrackObjectType Routine
        {
            get => this.Properties.Routine;
            set => this.Properties.Routine = value;
        }

        public ByteArray PaletteIndexes => this.Properties.PaletteIndexes;

        public Palette Palette => this.Properties.Palette;

        public bool Flashing
        {
            get => this.Properties.Flashing;
            set => this.Properties.Flashing = value;
        }

        public TrackObjectLoading Loading => this.Properties.Loading;

        public TrackObjects(byte[] data, byte[] areaData, TrackAI ai, byte[] propData, GPTrack track)
        {
            this.objects = new TrackObject[Size / BytesPerObject];
            this.SetBytes(data);

            this.Areas = new TrackObjectAreas(areaData, ai);
            this.Areas.PropertyChanged += this.SubPropertyChanged;

            this.Properties = new TrackObjectProperties(propData, track);
            this.Properties.PropertyChanged += this.SubPropertyChanged;
        }

        public void SetBytes(byte[] data)
        {
            if (data.Length != Size)
            {
                throw new ArgumentException("Incorrect track object data size", nameof(data));
            }

            for (int i = 0; i < RegularObjectCount; i++)
            {
                if (this.objects[i] != null)
                {
                    this.objects[i].PropertyChanged -= this.SubPropertyChanged;
                }

                this.objects[i] = new TrackObject(data, i * BytesPerObject);
                this.objects[i].PropertyChanged += this.SubPropertyChanged;
            }

            for (int i = RegularObjectCount; i < ObjectCount; i++)
            {
                if (this.objects[i] != null)
                {
                    this.objects[i].PropertyChanged -= this.SubPropertyChanged;
                }

                this.objects[i] = new TrackObjectMatchRace(data, i * BytesPerObject);
                this.objects[i].PropertyChanged += this.SubPropertyChanged;
            }
        }

        private void SubPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.OnPropertyChanged(sender, e);
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.PropertyChanged?.Invoke(sender, e);
        }

        public IEnumerator<TrackObject> GetEnumerator()
        {
            foreach (TrackObject tObject in this.objects)
            {
                yield return tObject;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.objects.GetEnumerator();
        }

        public int Count => this.objects.Length;

        public TrackObject this[int index] => this.objects[index];

        /// <summary>
        /// Returns the TrackObjects data as a byte array, in the format the SMK ROM expects.
        /// </summary>
        /// <returns>The TrackObjects bytes.</returns>
        public byte[] GetBytes()
        {
            byte[] data = new byte[this.objects.Length * BytesPerObject];

            for (int i = 0; i < this.objects.Length; i++)
            {
                this.objects[i].GetBytes(data, i * BytesPerObject);
            }

            return data;
        }
    }
}
