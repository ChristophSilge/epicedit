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

using EpicEdit.Rom.Settings;
using EpicEdit.Rom.Tracks.Overlay;
using EpicEdit.Rom.Tracks.Start;

namespace EpicEdit.Rom.Tracks
{
    /// <summary>
    /// A Battle track (Battle Mode).
    /// </summary>
    internal class BattleTrack : Track
    {
        /// <summary>
        /// Number of battle tracks.
        /// </summary>
        public new const int Count = 4;

        /// <summary>
        /// Number of battle track groups.
        /// </summary>
        public new const int GroupCount = 1;

        private readonly BattleStartPosition startPositionP1;

        /// <summary>
        /// The starting position of Player 1.
        /// </summary>
        public BattleStartPosition StartPositionP1
        {
            get => this.startPositionP1;
            private set => this.startPositionP1.SetBytes(value.GetBytes());
        }

        private readonly BattleStartPosition startPositionP2;

        /// <summary>
        /// The starting position of Player 2.
        /// </summary>
        public BattleStartPosition StartPositionP2
        {
            get => this.startPositionP2;
            private set => this.startPositionP2.SetBytes(value.GetBytes());
        }

        public BattleTrack(SuffixedTextItem nameItem, Theme theme,
                           byte[] map, byte[] overlayTileData,
                           byte[] aiAreaData, byte[] aiTargetData,
                           byte[] startPositionData,
                           OverlayTileSizes overlayTileSizes,
                           OverlayTilePatterns overlayTilePatterns) :
            base(nameItem, theme, map, overlayTileData, aiAreaData, aiTargetData, overlayTileSizes, overlayTilePatterns)
        {
            byte[] startPosition1Data = { startPositionData[0], startPositionData[1], startPositionData[2], startPositionData[3] };
            byte[] startPosition2Data = { startPositionData[4], startPositionData[5], startPositionData[6], startPositionData[7] };

            this.startPositionP1 = new BattleStartPosition(startPosition1Data);
            this.StartPositionP1.DataChanged += StartPositionP1_DataChanged;

            this.startPositionP2 = new BattleStartPosition(startPosition2Data);
            this.StartPositionP2.DataChanged += StartPositionP2_DataChanged;
        }

        private void StartPositionP1_DataChanged(object sender, System.EventArgs e)
        {
            this.MarkAsModified(PropertyNames.BattleTrack.StartPositionP1);
        }

        private void StartPositionP2_DataChanged(object sender, System.EventArgs e)
        {
            this.MarkAsModified(PropertyNames.BattleTrack.StartPositionP2);
        }

        /// <summary>
        /// Loads the BattleTrack-specific items from the MakeTrack object.
        /// </summary>
        protected override void LoadDataFrom(MakeTrack track)
        {
            base.LoadDataFrom(track);

            this.StartPositionP1 = track.StartPositionP1;
            this.StartPositionP2 = track.StartPositionP2;
        }

        /// <summary>
        /// Loads the BattleTrack-specific items to the MakeTrack object.
        /// </summary>
        protected override void LoadDataTo(MakeTrack track)
        {
            base.LoadDataTo(track);

            track.StartPositionP1 = this.StartPositionP1;
            track.StartPositionP2 = this.StartPositionP2;
        }
    }
}
