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
using EpicEdit.Rom.Tracks;
using EpicEdit.Rom.Tracks.AI;
using EpicEdit.Rom.Tracks.Objects;
using EpicEdit.Rom.Tracks.Overlay;
using EpicEdit.Rom.Tracks.Road;
using EpicEdit.Rom.Tracks.Start;
using EpicEdit.Rom.Utility;
using EpicEdit.UI.Tools;
using EpicEdit.UI.TrackEdition;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Region = System.Drawing.Region;

namespace EpicEdit.UI.Gfx
{
    /// <summary>
    /// Provides the ability to paint the graphics of a track.
    /// </summary>
    internal sealed class TrackDrawer : IDisposable
    {
        public event EventHandler<EventArgs> GraphicsChanged;

        private readonly TileClipboard tileClipboard;
        private Track track;

        private Point scrollPosition;
        public Point ScrollPosition
        {
            //get => this.scrollPosition;
            set => this.scrollPosition = value;
        }

        private float zoom;
        public float Zoom
        {
            //get => this.zoom;
            set
            {
                this.zoom = value;
                this.zoomMatrix.Reset();
                this.zoomMatrix.Scale(this.zoom, this.zoom);
            }
        }

        private Bitmap trackCache;
        private Bitmap tileClipboardCache;
        private Bitmap objectAreasCache;

        /// <summary>
        /// The object areas of the track.
        /// They're cached in order to figure out when to update the <see cref="objectAreasCache"/>.
        /// </summary>
        private byte[][] objectAreas;

        /// <summary>
        /// Used to resize the dirty region depending on the zoom level.
        /// </summary>
        private readonly Matrix zoomMatrix;

        /// <summary>
        /// Used to draw the rectangle when highlighting tiles.
        /// </summary>
        private readonly Pen tileHighlightPen;

        /// <summary>
        /// Used to draw the rectangle when selecting tiles.
        /// </summary>
        private readonly Pen tileSelectPen;

        /// <summary>
        /// Used to paint the inside of the selection rectangle.
        /// </summary>
        private readonly SolidBrush tileSelectBrush;

        /// <summary>
        /// Used to paint a rectangle over the hovered overlay tile.
        /// </summary>
        private readonly SolidBrush overlayHighlightBrush;

        /// <summary>
        /// Used to draw the lap line.
        /// </summary>
        private readonly Pen lapLinePen;

        /// <summary>
        /// Used to draw the lap line outline.
        /// </summary>
        private readonly Pen lapLineOutlinePen;

        /// <summary>
        /// Used to fill in arrows.
        /// </summary>
        private readonly SolidBrush arrowBrush;

        /// <summary>
        /// Used to draw arrow outlines.
        /// </summary>
        private readonly Pen arrowPen;

        /// <summary>
        /// Used to paint object outlines.
        /// </summary>
        private readonly Pen objectOutlinePen;

        /// <summary>
        /// Used to fill in objects depending on their group.
        /// </summary>
        private readonly SolidBrush[] objectBrushes;

        /// <summary>
        /// Used to fill in AI areas.
        /// </summary>
        private readonly SolidBrush[][] aiAreaBrushes;

        /// <summary>
        /// Used to draw the AI area outlines.
        /// </summary>
        private readonly Pen[] aiAreaPens;

        /// <summary>
        /// Used to draw the hovered AI area outlines.
        /// </summary>
        private readonly Pen aiElementHighlightPen;

        /// <summary>
        /// Used to draw the selected AI area outlines.
        /// </summary>
        private readonly Pen aiElementSelectPen;

        /// <summary>
        /// Image attributes to draw images as semi-transparent.
        /// </summary>
        private readonly ImageAttributes translucidImageAttr;

        /// <summary>
        /// Image attributes to draw images as grayed out / disabled
        /// </summary>
        private readonly ImageAttributes grayScaleImageAttr;

        public TrackDrawer(TileClipboard tileClipboard)
        {
            this.tileClipboard = tileClipboard;
            this.tileClipboard.TileChanged += (sender, e) => this.UpdateTileClipboardCache(e.Value);
            this.tileClipboard.TilesChanged += (sender, e) => this.UpdateTileClipboardCache(e.Value);

            #region Pens and Brushes initialization

            this.tileHighlightPen = new Pen(Color.FromArgb(150, 255, 0, 0), 1);
            this.tileSelectPen = new Pen(Color.FromArgb(150, 20, 130, 255), 1);
            this.tileSelectBrush = new SolidBrush(Color.FromArgb(50, 20, 130, 255));

            this.overlayHighlightBrush = new SolidBrush(Color.FromArgb(50, 255, 255, 255));

            this.lapLinePen = new Pen(Color.White);
            this.lapLineOutlinePen = new Pen(Color.Black, 3);
            this.arrowBrush = new SolidBrush(Color.White);
            this.arrowPen = new Pen(Color.Gray, 1);

            this.objectOutlinePen = new Pen(Color.White, 2);
            this.objectBrushes = new SolidBrush[]
            {
                new SolidBrush(Color.FromArgb(255, 204, 51, 51)), // Object area 1 object color
                new SolidBrush(Color.FromArgb(255, 21, 94, 177)), // Object area 2 object color
                new SolidBrush(Color.FromArgb(255, 230, 186, 64)), // Object area 3 object color
                new SolidBrush(Color.FromArgb(255, 16, 150, 24)) // Object area 4 object color
            };

            this.aiAreaBrushes = new SolidBrush[][]
            {
                new SolidBrush[]
                {
                    new SolidBrush(Color.FromArgb(60, 165, 204, 244)), // Top
                    new SolidBrush(Color.FromArgb(60, 125, 168, 255)), // Left / right
                    new SolidBrush(Color.FromArgb(60, 87, 126, 205)) // Bottom
                },

                new SolidBrush[]
                {
                    new SolidBrush(Color.FromArgb(60, 179, 244, 150)), // Top
                    new SolidBrush(Color.FromArgb(60, 141, 233, 109)), // Left / right
                    new SolidBrush(Color.FromArgb(60, 101, 185, 72)) // Bottom
                },

                new SolidBrush[]
                {
                    new SolidBrush(Color.FromArgb(60, 244, 231, 124)), // Top
                    new SolidBrush(Color.FromArgb(60, 228, 198, 80)), // Left / right
                    new SolidBrush(Color.FromArgb(60, 180, 153, 46)) // Bottom
                },

                new SolidBrush[]
                {
                    new SolidBrush(Color.FromArgb(60, 244, 172, 133)), // Top
                    new SolidBrush(Color.FromArgb(60, 222, 133, 90)), // Left / right
                    new SolidBrush(Color.FromArgb(60, 175, 94, 55)) // Bottom
                }
            };

            this.aiAreaPens = new Pen[]
            {
                new Pen(Color.FromArgb(255, 165, 204, 244), 1),
                new Pen(Color.FromArgb(255, 179, 244, 150), 1),
                new Pen(Color.FromArgb(255, 244, 231, 124), 1),
                new Pen(Color.FromArgb(255, 244, 172, 133), 1)
            };

            this.aiElementHighlightPen = new Pen(Color.FromArgb(150, 255, 255, 255), 1);
            this.aiElementSelectPen = new Pen(Color.White, 1);

            #endregion Pens and Brushes initialization

            this.translucidImageAttr = new ImageAttributes();
            ColorMatrix matrix = new ColorMatrix(new float[][]
            {
                new float[] { 1.0f, 0.0f, 0.0f, 0.0f, 0.0f },
                new float[] { 0.0f, 1.0f, 0.0f, 0.0f, 0.0f },
                new float[] { 0.0f, 0.0f, 1.0f, 0.0f, 0.0f },
                new float[] { 0.0f, 0.0f, 0.0f, 0.58f, 0.0f },
                new float[] { 0.0f, 0.0f, 0.0f, 0.0f, 1.0f }
            });
            this.translucidImageAttr.SetColorMatrix(matrix);

            this.grayScaleImageAttr = new ImageAttributes();
            matrix = new ColorMatrix(new float[][]
            {
                new float[] { 0.22f, 0.22f, 0.22f, 0.0f, 0.0f },
                new float[] { 0.27f, 0.27f, 0.27f, 0.0f, 0.0f },
                new float[] { 0.04f, 0.04f, 0.04f, 0.0f, 0.0f },
                new float[] { 0.365f, 0.365f, 0.365f, 0.7f, 0.0f },
                new float[] { 0.0f, 0.0f, 0.0f, 0.0f, 1.0f }
            });
            this.grayScaleImageAttr.SetColorMatrix(matrix);

            this.zoomMatrix = new Matrix();
            this.Zoom = 1;

            // The following members are initialized so they can be disposed of
            // in each function without having to check if they're null beforehand
            this.trackCache = this.tileClipboardCache = this.objectAreasCache = new Bitmap(1, 1, PixelFormat.Format32bppPArgb);
        }

        /// <summary>
        /// Loads a track and generates the track image cache.
        /// </summary>
        /// <param name="track">The track.</param>
        public void LoadTrack(Track track)
        {
            bool sameTheme = false;

            if (this.track != null)
            {
                sameTheme = this.track.Theme == track.Theme;
                this.track.PropertyChanged -= this.track_PropertyChanged;
                this.track.ColorGraphicsChanged -= this.track_ColorGraphicsChanged;
                this.track.ColorsGraphicsChanged -= this.track_ColorsGraphicsChanged;
            }

            this.track = track;

            this.track.PropertyChanged += this.track_PropertyChanged;
            this.track.ColorGraphicsChanged += this.track_ColorGraphicsChanged;
            this.track.ColorsGraphicsChanged += this.track_ColorsGraphicsChanged;

            this.CreateCache();

            if (!sameTheme)
            {
                this.CreateTileClipboardCache();
            }
        }

        private void track_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == PropertyNames.Track.Theme)
            {
                this.CreateTileClipboardCache();
            }
        }

        private void track_ColorGraphicsChanged(object sender, EventArgs<int> e)
        {
            Palette palette = sender as Palette;
            if (palette.Index < Palettes.SpritePaletteStart)
            {
                bool[] tilesToBeUpdated = this.GetTilesToBeUpdated(palette, e.Value);
                this.UpdateCache(this.trackCache, this.track.Map, tilesToBeUpdated);
                this.UpdateCache(this.tileClipboardCache, this.tileClipboard, tilesToBeUpdated);
            }

            if (e.Value == 0 && palette.Index != 0)
            {
                // HACK: Performance optimization.
                // When changing the first color of the first palette, only raise
                // the OnGraphicsChanged event once instead of 16 times (once per palette).
                // This event handler will be called in order by palettes from index 1 to 15,
                // then palette 0 is last. Only the last call will raise the OnGraphicsChanged event.
                return;
            }

            this.OnGraphicsChanged();
        }

        private void track_ColorsGraphicsChanged(object sender, EventArgs e)
        {
            Palette palette = sender as Palette;
            if (palette.Index < Palettes.SpritePaletteStart)
            {
                this.UpdateCache(this.trackCache, this.track.Map, palette);
                this.UpdateCache(this.tileClipboardCache, this.tileClipboard, palette);
            }

            this.OnGraphicsChanged();
        }

        private void OnGraphicsChanged()
        {
            this.GraphicsChanged?.Invoke(this, EventArgs.Empty);
        }

        private Region GetZoomedRegion(Region region)
        {
            if (this.zoom != 1)
            {
                region.Transform(this.zoomMatrix);
            }

            return region;
        }

        private Region GetTranslatedZoomedRegion(Rectangle rectangle)
        {
            return this.GetTranslatedZoomedRegion(new Region(rectangle));
        }

        private Region GetTranslatedZoomedRegion(Region region)
        {
            region.Translate(-this.scrollPosition.X * Tile.Size, -this.scrollPosition.Y * Tile.Size);
            return this.GetZoomedRegion(region);
        }

        /// <summary>
        /// Reloads the current track and regenerates the track image cache.
        /// </summary>
        public void CreateCache()
        {
            this.CreateCache(ref this.trackCache, this.track.Map);
        }

        private void CreateCache(ref Bitmap cache, IMapBuffer mapBuffer)
        {
            cache.Dispose();

            cache = new Bitmap(mapBuffer.Width * Tile.Size, mapBuffer.Height * Tile.Size, PixelFormat.Format32bppPArgb);

            RoadTileset tileset = this.track.RoadTileset;

            using (Graphics g = Graphics.FromImage(cache))
            {
                for (int x = 0; x < mapBuffer.Width; x++)
                {
                    for (int y = 0; y < mapBuffer.Height; y++)
                    {
                        Tile tile = tileset[mapBuffer[x, y]];
                        g.DrawImage(tile.Bitmap, x * Tile.Size, y * Tile.Size);
                    }
                }
            }
        }

        private void UpdateCache(Image cache, IMapBuffer mapBuffer, Palette palette)
        {
            RoadTileset tileset = this.track.RoadTileset;

            using (Graphics g = Graphics.FromImage(cache))
            {
                for (int x = 0; x < mapBuffer.Width; x++)
                {
                    for (int y = 0; y < mapBuffer.Height; y++)
                    {
                        Tile tile = tileset[mapBuffer[x, y]];
                        if (tile.Palette == palette)
                        {
                            g.DrawImage(tile.Bitmap, x * Tile.Size, y * Tile.Size);
                        }
                    }
                }
            }
        }

        private void UpdateCache(Image cache, IMapBuffer mapBuffer, bool[] tilesToBeUpdated)
        {
            RoadTileset tileset = this.track.RoadTileset;

            using (Graphics g = Graphics.FromImage(cache))
            {
                for (int x = 0; x < mapBuffer.Width; x++)
                {
                    for (int y = 0; y < mapBuffer.Height; y++)
                    {
                        byte tileId = mapBuffer[x, y];
                        if (tilesToBeUpdated[tileId])
                        {
                            Tile tile = tileset[tileId];
                            g.DrawImage(tile.Bitmap, x * Tile.Size, y * Tile.Size);
                        }
                    }
                }
            }
        }

        public void UpdateCache(byte tileId)
        {
            Tile tile = this.track.RoadTileset[tileId];
            TrackMap trackMap = this.track.Map;

            using (Graphics g = Graphics.FromImage(this.trackCache))
            {
                for (int x = 0; x < trackMap.Width; x++)
                {
                    for (int y = 0; y < trackMap.Height; y++)
                    {
                        if (trackMap[x, y] == tileId)
                        {
                            g.DrawImage(tile.Bitmap, x * Tile.Size, y * Tile.Size);
                        }
                    }
                }
            }
        }

        public Region UpdateCache(Rectangle rectangle)
        {
            RoadTileset tileset = this.track.RoadTileset;
            TrackMap trackMap = this.track.Map;

            using (Graphics g = Graphics.FromImage(this.trackCache))
            {
                for (int x = rectangle.X; x < rectangle.Right; x++)
                {
                    for (int y = rectangle.Y; y < rectangle.Bottom; y++)
                    {
                        Tile tile = tileset[trackMap[x, y]];
                        g.DrawImage(tile.Bitmap, x * Tile.Size, y * Tile.Size);
                    }
                }
            }

            Rectangle dirtyRectangle = new Rectangle(rectangle.X * Tile.Size,
                                                     rectangle.Y * Tile.Size,
                                                     rectangle.Width * Tile.Size,
                                                     rectangle.Height * Tile.Size);

            return this.GetTranslatedZoomedRegion(dirtyRectangle);
        }

        public void UpdateCacheOnTileChange(Point absolutePosition)
        {
            using (Graphics g = Graphics.FromImage(this.trackCache))
            {
                g.DrawImage(this.tileClipboardCache, absolutePosition.X * Tile.Size, absolutePosition.Y * Tile.Size);
            }
        }

        private void UpdateTileClipboardCache(byte tile)
        {
            this.tileClipboardCache.Dispose();
            this.tileClipboardCache = this.track.RoadTileset[tile].Bitmap.Clone() as Bitmap;
        }

        public void UpdateTileClipboardCache(Rectangle rectangle)
        {
            this.tileClipboardCache.Dispose();

            Rectangle clipboardRectangle = new Rectangle(
                rectangle.X * Tile.Size,
                rectangle.Y * Tile.Size,
                rectangle.Width * Tile.Size,
                rectangle.Height * Tile.Size);

            this.tileClipboardCache = this.trackCache.Clone(clipboardRectangle, this.trackCache.PixelFormat);
        }

        private void CreateTileClipboardCache()
        {
            this.CreateCache(ref this.tileClipboardCache, this.tileClipboard);
        }

        private bool[] GetTilesToBeUpdated(Palette palette, int colorIndex)
        {
            bool[] tilesToBeUpdated = new bool[RoadTileset.TileCount];

            for (int i = 0; i < tilesToBeUpdated.Length; i++)
            {
                Tile tile = this.track.RoadTileset[i];
                if (tile.Palette == palette && tile.Contains(colorIndex))
                {
                    tilesToBeUpdated[i] = true;
                }
            }

            return tilesToBeUpdated;
        }

        private Rectangle GetTrackClip(Rectangle bounds)
        {
            bool xDiff = bounds.X > ((int)(bounds.X / this.zoom) * this.zoom);
            bool yDiff = bounds.Y > ((int)(bounds.Y / this.zoom) * this.zoom);

            bounds.X = (int)(bounds.X / this.zoom) + this.scrollPosition.X * Tile.Size;
            bounds.Y = (int)(bounds.Y / this.zoom) + this.scrollPosition.Y * Tile.Size;

            // Sometimes increase the width or height by 1px to make up for rounding differences.
            // Happens when dividing then multiplying X or Y with the zoom value
            // results in a smaller integer value (e.g: (int)(5 / 2) * 2 = 4).
            bounds.Width = (int)Math.Ceiling(bounds.Width / this.zoom) + (xDiff ? 1 : 0);
            bounds.Height = (int)Math.Ceiling(bounds.Height / this.zoom) + (yDiff ? 1 : 0);

            if (bounds.Right > this.track.Map.Width * Tile.Size)
            {
                bounds.Width = this.track.Map.Width * Tile.Size - bounds.X;
            }

            if (bounds.Bottom > this.track.Map.Height * Tile.Size)
            {
                bounds.Height = this.track.Map.Height * Tile.Size - bounds.Y;
            }

            return bounds;
        }

        private Bitmap CreateClippedTrackImage(Rectangle clip)
        {
            return this.trackCache.Clone(clip, this.trackCache.PixelFormat);
        }

        private static Graphics CreateBackBuffer(Image image, Rectangle clip)
        {
            Graphics backBuffer = Graphics.FromImage(image);
            backBuffer.TranslateTransform(-clip.X, -clip.Y);
            return backBuffer;
        }

        private void DrawImage(Graphics g, Bitmap image, Rectangle clip)
        {
            // Solves a GDI+ bug which crops scaled images
            g.PixelOffsetMode = PixelOffsetMode.Half;

            g.InterpolationMode = this.zoom >= 1 ?
                InterpolationMode.NearestNeighbor :
                InterpolationMode.Bilinear;

            g.DrawImage(image,
                        (clip.X - this.scrollPosition.X * Tile.Size) * this.zoom,
                        (clip.Y - this.scrollPosition.Y * Tile.Size) * this.zoom,
                        clip.Width * this.zoom,
                        clip.Height * this.zoom);
        }

        public Region GetTrackTilesetRegion(Rectangle tileSelection)
        {
            Region region;

            if (tileSelection.IsEmpty)
            {
                region = new Region(tileSelection);
            }
            else
            {
                Rectangle visibleSelection = tileSelection.IsEmpty ? Rectangle.Empty :
                    TrackDrawer.GetVisibleTileSelectionRectangle(tileSelection);

                // Enlarge rectangle by 1px to account for the 1px border of the selection
                visibleSelection.Inflate(1, 1);

                region = this.GetTranslatedZoomedRegion(visibleSelection);
            }

            return region;
        }

        public Region GetTrackOverlayRegion(OverlayTile hoveredOverlayTile, OverlayTile selectedOverlayTile, OverlayTilePattern selectedPattern, Point selectedPatternLocation)
        {
            Region region = new Region(Rectangle.Empty);

            if (hoveredOverlayTile != null)
            {
                Rectangle rec = TrackDrawer.GetOverlayClipRectangle(hoveredOverlayTile.Pattern, hoveredOverlayTile.Location);
                region.Union(rec);
            }

            if (selectedOverlayTile != null &&
                selectedOverlayTile != hoveredOverlayTile)
            {
                Rectangle rec = TrackDrawer.GetOverlayClipRectangle(selectedOverlayTile.Pattern, selectedOverlayTile.Location);
                region.Union(rec);
            }

            if (selectedPattern != null &&
                selectedPatternLocation != TrackEditor.OutOfBounds)
            {
                Rectangle rec = TrackDrawer.GetOverlayClipRectangle(selectedPattern, selectedPatternLocation);
                region.Union(rec);
            }

            region = this.GetTranslatedZoomedRegion(region);

            return region;
        }

        public Region GetTrackStartRegion()
        {
            Region region;

            if (this.track is GPTrack gpTrack)
            {
                region = this.GetGPStartClipRegion(gpTrack.LapLine, gpTrack.StartPosition);
            }
            else
            {
                BattleTrack bTrack = this.track as BattleTrack;
                region = TrackDrawer.GetBattleStartClipRegion(bTrack.StartPositionP1, bTrack.StartPositionP2);
            }

            region = this.GetTranslatedZoomedRegion(region);

            return region;
        }

        public Region GetTrackObjectsRegion(TrackObject hoveredObject)
        {
            Region region;

            if (hoveredObject == null)
            {
                region = new Region(Rectangle.Empty);
            }
            else
            {
                int x = hoveredObject.X * Tile.Size - Tile.Size;
                int y = hoveredObject.Y * Tile.Size - (Tile.Size + Tile.Size / 2);
                int width = 24;
                int height = 24;

                if (hoveredObject is TrackObjectMatchRace)
                {
                    x -= 6;
                    y -= 6;
                    width += 12;
                    height += 12;
                }

                Rectangle hoveredObjectRectangle = new Rectangle(x, y, width, height);

                region = this.GetTranslatedZoomedRegion(hoveredObjectRectangle);
            }

            return region;
        }

        public Region GetTrackAIRegion(TrackAIElement hoveredAIElem, TrackAIElement selectedAIElem)
        {
            Region region = new Region(Rectangle.Empty);

            if (hoveredAIElem != null)
            {
                region.Union(TrackDrawer.GetAIClipRectangle(hoveredAIElem));
            }

            if (selectedAIElem != null &&
                selectedAIElem != hoveredAIElem)
            {
                region.Union(TrackDrawer.GetAIClipRectangle(selectedAIElem));
            }

            region = this.GetTranslatedZoomedRegion(region);

            return region;
        }

        public void DrawTrackTileset(PaintEventArgs e, Rectangle tileSelection, bool selectingTiles, bool bucketMode)
        {
            Rectangle clip = this.GetTrackClip(e.ClipRectangle);

            if (clip.Width > 0 && clip.Height > 0)
            {
                using (Bitmap image = this.CreateClippedTrackImage(clip))
                {
                    if (!tileSelection.IsEmpty)
                    {
                        if (bucketMode)
                        {
                            tileSelection.Width = 1;
                            tileSelection.Height = 1;
                        }

                        using (Graphics backBuffer = TrackDrawer.CreateBackBuffer(image, clip))
                        {
                            this.DrawTileSelection(backBuffer, tileSelection, selectingTiles);
                        }
                    }

                    this.DrawImage(e.Graphics, image, clip);
                }
            }

            this.PaintTrackOutbounds(e);
        }

        public void DrawTrackOverlay(PaintEventArgs e, OverlayTile hoveredOverlayTile, OverlayTile selectedOverlayTile, OverlayTilePattern selectedPattern, Point selectedPatternLocation)
        {
            Rectangle clip = this.GetTrackClip(e.ClipRectangle);

            if (clip.Width > 0 && clip.Height > 0)
            {
                using (Bitmap image = this.CreateClippedTrackImage(clip))
                using (Graphics backBuffer = TrackDrawer.CreateBackBuffer(image, clip))
                {
                    this.DrawOverlay(backBuffer, hoveredOverlayTile, selectedOverlayTile, selectedPattern, selectedPatternLocation);
                    this.DrawImage(e.Graphics, image, clip);
                }
            }

            this.PaintTrackOutbounds(e);
        }

        public void DrawTrackStart(PaintEventArgs e)
        {
            Rectangle clip = this.GetTrackClip(e.ClipRectangle);

            if (clip.Width > 0 && clip.Height > 0)
            {
                using (Bitmap image = this.CreateClippedTrackImage(clip))
                using (Graphics backBuffer = TrackDrawer.CreateBackBuffer(image, clip))
                {
                    this.DrawStartData(backBuffer);
                    this.DrawImage(e.Graphics, image, clip);
                }
            }

            this.PaintTrackOutbounds(e);
        }

        public void DrawTrackObjects(PaintEventArgs e, TrackObject hoveredObject, bool frontAreasView)
        {
            Rectangle clip = this.GetTrackClip(e.ClipRectangle);

            if (clip.Width > 0 && clip.Height > 0)
            {
                using (Bitmap image = this.CreateClippedTrackImage(clip))
                {
                    if (this.track is GPTrack)
                    {
                        using (Graphics backBuffer = TrackDrawer.CreateBackBuffer(image, clip))
                        {
                            this.DrawObjectData(backBuffer, hoveredObject, frontAreasView);
                        }
                    }

                    this.DrawImage(e.Graphics, image, clip);
                }
            }

            this.PaintTrackOutbounds(e);
        }

        public void DrawTrackAI(PaintEventArgs e, TrackAIElement hoveredAIElem, TrackAIElement selectedAIElem, bool isAITargetHovered)
        {
            Rectangle clip = this.GetTrackClip(e.ClipRectangle);

            if (clip.Width > 0 && clip.Height > 0)
            {
                using (Bitmap image = this.CreateClippedTrackImage(clip))
                using (Graphics backBuffer = TrackDrawer.CreateBackBuffer(image, clip))
                {
                    this.DrawAI(backBuffer, hoveredAIElem, selectedAIElem, isAITargetHovered);
                    this.DrawImage(e.Graphics, image, clip);
                }
            }

            this.PaintTrackOutbounds(e);
        }

        private static Rectangle GetVisibleTileSelectionRectangle(Rectangle tileSelection)
        {
            return new Rectangle(tileSelection.X * Tile.Size - 1,
                                 tileSelection.Y * Tile.Size - 1,
                                 tileSelection.Width * Tile.Size + 1,
                                 tileSelection.Height * Tile.Size + 1);
        }

        private static Rectangle GetOverlayClipRectangle(OverlayTilePattern overlayTilePattern, Point location)
        {
            return new Rectangle(location.X * Tile.Size,
                                 location.Y * Tile.Size,
                                 overlayTilePattern.Width * Tile.Size,
                                 overlayTilePattern.Height * Tile.Size);
        }

        private Region GetGPStartClipRegion(LapLine lapLine, GPStartPosition startPosition)
        {
            Rectangle lapLineRectangle =
                new Rectangle(lapLine.X,
                              lapLine.Y - 1,
                              lapLine.Length, 3);

            Rectangle startRectangle1 = new Rectangle(startPosition.X - 4,
                                                      startPosition.Y - 5,
                                                      Tile.Size + 1,
                                                      GPStartPosition.Height - 14);

            Rectangle startRectangle2 = new Rectangle(startRectangle1.X + startPosition.SecondRowOffset,
                                                      startRectangle1.Y + 24,
                                                      startRectangle1.Width,
                                                      startRectangle1.Height);

            if (this.zoom < 1)
            {
                // HACK: Avoid clipping issues (rounding differences)
                startRectangle1.Inflate(1, 0);
                startRectangle2.Inflate(1, 0);
            }

            if (!Platform.IsWindows)
            {
                // HACK: Workaround for differences with Mono out of Windows
                // (differently-sized lap line / arrow graphics).
                lapLineRectangle.X++;
                startRectangle1.Y--;
                startRectangle2.Y--;
            }

            Region region = new Region(lapLineRectangle);
            region.Union(startRectangle1);
            region.Union(startRectangle2);

            return region;
        }

        private static Region GetBattleStartClipRegion(BattleStartPosition startPositionP1, BattleStartPosition startPositionP2)
        {
            Region region = new Region(TrackDrawer.GetBattleStartClipRectangle(startPositionP1));
            region.Union(TrackDrawer.GetBattleStartClipRectangle(startPositionP2));
            return region;
        }

        private static Rectangle GetBattleStartClipRectangle(BattleStartPosition startPosition)
        {
            return new Rectangle(startPosition.X - 4,
                                 startPosition.Y - 4,
                                 Tile.Size + 1, Tile.Size + 1);
        }

        /// <summary>
        /// Gets the rectangle that includes the whole AI element (area + target).
        /// </summary>
        /// <param name="aiElement">The AI element.</param>
        /// <returns>The rectangle that includes the whole AI element (area + target).</returns>
        private static Rectangle GetAIClipRectangle(TrackAIElement aiElement)
        {
            int x;
            int y;
            int width;
            int height;

            if (aiElement.Area.X <= aiElement.Target.X)
            {
                x = aiElement.Area.X;
                if (aiElement.Target.X >= aiElement.Area.Right)
                {
                    width = aiElement.Target.X - aiElement.Area.Left + 1;
                }
                else
                {
                    width = aiElement.Area.Width;
                }
            }
            else
            {
                x = aiElement.Target.X;
                width = aiElement.Area.Right - aiElement.Target.X;
            }

            if (aiElement.Area.Y <= aiElement.Target.Y)
            {
                y = aiElement.Area.Y;
                if (aiElement.Target.Y >= aiElement.Area.Bottom)
                {
                    height = aiElement.Target.Y - aiElement.Area.Top + 1;
                }
                else
                {
                    height = aiElement.Area.Height;
                }
            }
            else
            {
                y = aiElement.Target.Y;
                height = aiElement.Area.Bottom - aiElement.Target.Y;
            }

            Rectangle hoveredAIElemRectangle = new Rectangle(x * Tile.Size,
                                                             y * Tile.Size,
                                                             width * Tile.Size,
                                                             height * Tile.Size);

            return hoveredAIElemRectangle;
        }

        private void DrawTileSelection(Graphics g, Rectangle tileSelection, bool selectingTiles)
        {
            Rectangle visibleSelection = TrackDrawer.GetVisibleTileSelectionRectangle(tileSelection);

            if (selectingTiles) // A tile selection is happening now
            {
                g.FillRectangle(this.tileSelectBrush, visibleSelection);
                g.DrawRectangle(this.tileSelectPen, visibleSelection);
            }
            else // The user is simply hovering tiles
            {
                g.DrawImage(this.tileClipboardCache,
                            new Rectangle(visibleSelection.X + 1,
                                          visibleSelection.Y + 1,
                                          visibleSelection.Width,
                                          visibleSelection.Height),
                            0, 0, visibleSelection.Width, visibleSelection.Height,
                            GraphicsUnit.Pixel, this.translucidImageAttr);
                g.DrawRectangle(this.tileHighlightPen, visibleSelection);
            }
        }

        private void DrawOverlay(Graphics g, OverlayTile hoveredOverlayTile, OverlayTile selectedOverlayTile, OverlayTilePattern selectedPattern, Point selectedPatternLocation)
        {
            RoadTileset tileset = this.track.RoadTileset;

            foreach (OverlayTile overlayTile in this.track.OverlayTiles)
            {
                TrackDrawer.DrawOverlayTile(g, overlayTile, tileset);
            }

            if (hoveredOverlayTile != null)
            {
                this.DrawOverlaySelection(g, hoveredOverlayTile);
            }

            if (selectedOverlayTile != null &&
                selectedOverlayTile != hoveredOverlayTile)
            {
                this.DrawOverlaySelection(g, selectedOverlayTile);
            }

            if (selectedPattern != null && selectedPatternLocation != TrackEditor.OutOfBounds)
            {
                this.DrawOverlayPattern(g, selectedPattern, selectedPatternLocation, tileset);
            }
        }

        private static void DrawOverlayTile(Graphics g, OverlayTile overlayTile, RoadTileset tileset)
        {
            TrackDrawer.DrawOverlayTileSub(g, null, overlayTile.Pattern, overlayTile.Location, tileset);
        }

        private void DrawOverlayPattern(Graphics g, OverlayTilePattern overlayTilePattern, Point location, RoadTileset tileset)
        {
            TrackDrawer.DrawOverlayTileSub(g, this.translucidImageAttr, overlayTilePattern, location, tileset);
        }

        private static void DrawOverlayTileSub(Graphics g, ImageAttributes imageAttr, OverlayTilePattern overlayTilePattern, Point location, RoadTileset tileset)
        {
            for (int x = 0; x < overlayTilePattern.Width; x++)
            {
                for (int y = 0; y < overlayTilePattern.Height; y++)
                {
                    byte tileId = overlayTilePattern[x, y];
                    if (tileId == OverlayTile.None)
                    {
                        continue;
                    }

                    Tile tile = tileset[tileId];

                    g.DrawImage(tile.Bitmap,
                                new Rectangle((location.X + x) * Tile.Size,
                                              (location.Y + y) * Tile.Size,
                                              Tile.Size, Tile.Size),
                                0, 0, Tile.Size, Tile.Size,
                                GraphicsUnit.Pixel, imageAttr);
                }
            }
        }

        private void DrawOverlaySelection(Graphics g, OverlayTile overlayTile)
        {
            g.FillRectangle(this.overlayHighlightBrush,
                            overlayTile.X * Tile.Size,
                            overlayTile.Y * Tile.Size,
                            overlayTile.Width * Tile.Size,
                            overlayTile.Height * Tile.Size);
        }

        private void DrawStartData(Graphics g)
        {
            if (this.track is GPTrack)
            {
                this.DrawLapLine(g);
                this.DrawGPStartPositions(g);
            }
            else
            {
                this.DrawBattleStartPositions(g);
            }
        }

        private void DrawLapLine(Graphics g)
        {
            GPTrack gpTrack = this.track as GPTrack;

            Point location = new Point(gpTrack.LapLine.X, gpTrack.LapLine.Y);

            g.DrawLine(this.lapLineOutlinePen, location.X, location.Y,
                       location.X + gpTrack.LapLine.Length, location.Y);

            g.DrawLine(this.lapLinePen, location.X + 1, location.Y,
                       location.X + gpTrack.LapLine.Length - 2, location.Y);
        }

        private void DrawGPStartPositions(Graphics g)
        {
            GPTrack gpTrack = this.track as GPTrack;

            int x = gpTrack.StartPosition.X;
            int y = gpTrack.StartPosition.Y;
            int secondRowOffset = gpTrack.StartPosition.SecondRowOffset;

            for (int pos = 0; pos <= 18; pos += 6)
            {
                // 1st Column
                this.DrawUpArrow(g, x, y + pos * Tile.Size);

                // 2nd Column
                this.DrawUpArrow(g, x + secondRowOffset, y + ((3 + pos) * Tile.Size));
            }
        }

        private void DrawBattleStartPositions(Graphics g)
        {
            BattleTrack bTrack = this.track as BattleTrack;

            this.DrawDownArrow(g, bTrack.StartPositionP2.X, bTrack.StartPositionP2.Y);
            this.DrawUpArrow(g, bTrack.StartPositionP1.X, bTrack.StartPositionP1.Y);
        }

        private void DrawUpArrow(Graphics g, int x, int y)
        {
            Point[] arrow = new Point[]
            {
                new Point(x, y - 4), // Top center (tip)
                new Point(x + 4, y + 4), // Bottom right
                new Point(x - 4, y + 4) // Bottom left
            };
            this.DrawArrow(g, arrow);
        }

        private void DrawDownArrow(Graphics g, int x, int y)
        {
            Point[] arrow = new Point[]
            {
                new Point(x - 4, y - 4), // Top left
                new Point(x + 4, y - 4), // Top right
                new Point(x, y + 4) // Bottom center (tip)
            };
            this.DrawArrow(g, arrow);
        }

        private void DrawLeftArrow(Graphics g, int x, int y)
        {
            Point[] arrow = new Point[]
            {
                new Point(x - 4, y), // Center left (tip)
                new Point(x + 4, y + 4), // Bottom right
                new Point(x + 4, y - 4) // Top right
            };
            this.DrawArrow(g, arrow);
        }

        private void DrawRightArrow(Graphics g, int x, int y)
        {
            Point[] arrow = new Point[]
            {
                new Point(x - 4, y - 4), // Top left
                new Point(x - 4, y + 4), // Bottom left
                new Point(x + 4, y) // Center right (tip)
            };
            this.DrawArrow(g, arrow);
        }

        private void DrawArrow(Graphics g, Point[] arrow)
        {
            g.FillPolygon(this.arrowBrush, arrow);
            g.DrawPolygon(this.arrowPen, arrow);
        }

        private void DrawObjectData(Graphics g, TrackObject hoveredObject, bool frontAreasView)
        {
            GPTrack gpTrack = this.track as GPTrack;

            if (gpTrack.Objects.Routine != TrackObjectType.Pillar)
            {
                g.PixelOffsetMode = PixelOffsetMode.Half;

                this.DrawObjectAreas(g, frontAreasView);

                TrackObjectGraphics objectGraphics = Context.Game.ObjectGraphics;
                using (Bitmap objectImage = objectGraphics.GetImage(gpTrack))
                using (Bitmap matchRaceObjectImage = objectGraphics.GetMatchRaceObjectImage(gpTrack.Theme, true))
                using (Bitmap stillMatchRaceObjectImage = objectGraphics.GetMatchRaceObjectImage(gpTrack.Theme, false))
                {
                    this.DrawObjects(g, objectImage, matchRaceObjectImage, stillMatchRaceObjectImage, hoveredObject);
                }
            }
        }

        private void DrawObjectAreas(Graphics g, bool frontAreasView)
        {
            this.InitObjectAreasBitmap(frontAreasView);

            g.DrawImage(this.objectAreasCache,
                        new Rectangle(0, 0,
                                      this.track.Map.Width * Tile.Size,
                                      this.track.Map.Height * Tile.Size),
                        0, 0, TrackObjectAreasView.GridSize, TrackObjectAreasView.GridSize,
                        GraphicsUnit.Pixel, this.translucidImageAttr);
        }

        private void InitObjectAreasBitmap(bool frontAreasView)
        {
            GPTrack gpTrack = this.track as GPTrack;
            byte[][] objectAreas = frontAreasView ?
                gpTrack.Objects.Areas.FrontView.GetGrid() :
                gpTrack.Objects.Areas.RearView.GetGrid();

            if (this.ObjectAreasChanged(objectAreas))
            {
                this.objectAreas = objectAreas;
                this.objectAreasCache.Dispose();
                this.objectAreasCache = this.CreateObjectAreasBitmap();
            }
        }

        private bool ObjectAreasChanged(byte[][] objectAreas)
        {
            if (this.objectAreas == null)
            {
                return true;
            }

            for (int y = 0; y < objectAreas.Length; y++)
            {
                for (int x = 0; x < objectAreas[y].Length; x++)
                {
                    if (this.objectAreas[y][x] != objectAreas[y][x])
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Creates a small bitmap (64x64) containing colored areas which represent object areas.
        /// </summary>
        private Bitmap CreateObjectAreasBitmap()
        {
            Bitmap bitmap = new Bitmap(TrackObjectAreasView.GridSize, TrackObjectAreasView.GridSize, this.trackCache.PixelFormat);
            FastBitmap fBitmap = new FastBitmap(bitmap);

            for (int y = 0; y < this.objectAreas.Length; y++)
            {
                for (int x = 0; x < this.objectAreas[y].Length; x++)
                {
                    byte objectAreaIndex = this.objectAreas[y][x];
                    fBitmap.SetPixel(x, y, this.objectBrushes[objectAreaIndex].Color);
                }
            }

            fBitmap.Release();
            return bitmap;
        }

        private void DrawObjects(Graphics g, Bitmap objectImage, Bitmap matchRaceObjectImage, Bitmap stillMatchRaceObjectImage, TrackObject hoveredObject)
        {
            GPTrack gpTrack = this.track as GPTrack;

            int hoveredObjectIndex = 0;

            // 2P Match Race objects (Chain Chomps or Bananas)
            for (int i = TrackObjects.ObjectCount - 1; i >= TrackObjects.RegularObjectCount; i--)
            {
                TrackObjectMatchRace trackObject = gpTrack.Objects[i] as TrackObjectMatchRace;
                int x = trackObject.X * Tile.Size;
                int y = trackObject.Y * Tile.Size - (Tile.Size / 2);

                if (trackObject == hoveredObject)
                {
                    hoveredObjectIndex = i;
                }
                else
                {
                    Bitmap image = trackObject.Direction == TrackObjectDirection.None ?
                        stillMatchRaceObjectImage : matchRaceObjectImage;
                    g.DrawImage(image, x - (Tile.Size / 2), y - (Tile.Size / 2));
                }

                if (trackObject.Direction == TrackObjectDirection.Horizontal)
                {
                    this.DrawLeftArrow(g, x - 9, y + 4);
                    this.DrawRightArrow(g, x + 18, y + 4);
                }
                else if (trackObject.Direction == TrackObjectDirection.Vertical)
                {
                    this.DrawUpArrow(g, x + 4, y - 9);
                    this.DrawDownArrow(g, x + 4, y + 18);
                }
            }

            // Regular objects (Pipes, Piranha Plants...)
            for (int i = TrackObjects.RegularObjectCount - 1; i >= 0; i--)
            {
                TrackObject trackObject = gpTrack.Objects[i];
                int x = trackObject.X * Tile.Size;
                int y = trackObject.Y * Tile.Size - (Tile.Size / 2);

                if (trackObject == hoveredObject)
                {
                    hoveredObjectIndex = i;
                }
                else
                {
                    if (this.objectAreas[trackObject.Y / TrackAIElement.Precision]
                                  [trackObject.X / TrackAIElement.Precision] != (i / 4))
                    {
                        // The object is out of its area, it most likely won't
                        // (fully) appear when playing the game. Show it as grayed out.
                        g.DrawImage(objectImage,
                                    new Rectangle(x - (Tile.Size / 2),
                                                  y - (Tile.Size / 2),
                                                  16, 16),
                                    0, 0, 16, 16,
                                    GraphicsUnit.Pixel, this.grayScaleImageAttr);
                    }
                    else
                    {
                        g.DrawImage(objectImage, x - (Tile.Size / 2), y - (Tile.Size / 2));
                    }
                }
            }

            if (hoveredObject != null)
            {
                int x = hoveredObject.X * Tile.Size;
                int y = hoveredObject.Y * Tile.Size - (Tile.Size / 2);
                Rectangle trackObjectRect = new Rectangle(x - 6, y - 6, 20, 20);
                g.DrawEllipse(this.objectOutlinePen, trackObjectRect);

                if (!(hoveredObject is TrackObjectMatchRace matchRaceObject))
                {
                    g.FillEllipse(this.objectBrushes[hoveredObjectIndex / 4], trackObjectRect);
                    g.DrawImage(objectImage, x - (Tile.Size / 2), y - (Tile.Size / 2));
                }
                else
                {
                    Bitmap image = matchRaceObject.Direction == TrackObjectDirection.None ?
                        stillMatchRaceObjectImage : matchRaceObjectImage;
                    g.DrawImage(image, x - (Tile.Size / 2), y - (Tile.Size / 2));
                }
            }
        }

        private void DrawAI(Graphics g, TrackAIElement hoveredAIElem, TrackAIElement selectedAIElem, bool isAITargetHovered)
        {
            this.DrawAIElements(g);

            if (hoveredAIElem != selectedAIElem)
            {
                this.HighlightHoveredAIElement(g, hoveredAIElem, isAITargetHovered);
            }
            this.HighlightSelectedAIElement(g, selectedAIElem);
        }

        private void DrawAIElements(Graphics g)
        {
            foreach (TrackAIElement aiElem in this.track.AI)
            {
                this.DrawAIElement(g, aiElem);
            }
        }

        private void DrawAIElement(Graphics g, TrackAIElement aiElem)
        {
            const int HalfTileSize = Tile.Size / 2;

            int pointX = aiElem.Target.X * Tile.Size;
            int pointY = aiElem.Target.Y * Tile.Size;
            g.DrawEllipse(this.objectOutlinePen, pointX + 1, pointY + 1, HalfTileSize + 1, HalfTileSize + 1);

            Rectangle aiArea = TrackDrawer.GetAIAreaRectangle(aiElem);
            Point target = new Point(pointX + HalfTileSize, pointY + HalfTileSize);
            int speed = aiElem.Speed;

            if (aiElem.AreaShape == TrackAIElementShape.Rectangle)
            {
                TrackDrawer.PaintTopSide(g, this.aiAreaBrushes[speed][0], aiArea, target);
                TrackDrawer.PaintRightSide(g, this.aiAreaBrushes[speed][1], aiArea, target);
                TrackDrawer.PaintBottomSide(g, this.aiAreaBrushes[speed][2], aiArea, target);
                TrackDrawer.PaintLeftSide(g, this.aiAreaBrushes[speed][1], aiArea, target);

                g.DrawRectangle(this.aiAreaPens[speed], aiArea);
            }
            else
            {
                Point[] points = TrackDrawer.GetAIAreaTriangle(aiElem);

                g.DrawPolygon(this.aiAreaPens[speed], points);

                switch (aiElem.AreaShape)
                {
                    case TrackAIElementShape.TriangleTopLeft:
                        TrackDrawer.PaintTopSide(g, this.aiAreaBrushes[speed][0], aiArea, target);
                        TrackDrawer.PaintLeftSide(g, this.aiAreaBrushes[speed][1], aiArea, target);
                        TrackDrawer.PaintTriangleDiagonalSide(g, this.aiAreaBrushes[speed][2], points, target);
                        break;

                    case TrackAIElementShape.TriangleTopRight:
                        TrackDrawer.PaintTopSide(g, this.aiAreaBrushes[speed][0], aiArea, target);
                        TrackDrawer.PaintRightSide(g, this.aiAreaBrushes[speed][1], aiArea, target);
                        TrackDrawer.PaintTriangleDiagonalSide(g, this.aiAreaBrushes[speed][2], points, target);
                        break;

                    case TrackAIElementShape.TriangleBottomRight:
                        TrackDrawer.PaintBottomSide(g, this.aiAreaBrushes[speed][2], aiArea, target);
                        TrackDrawer.PaintRightSide(g, this.aiAreaBrushes[speed][1], aiArea, target);
                        TrackDrawer.PaintTriangleDiagonalSide(g, this.aiAreaBrushes[speed][0], points, target);
                        break;

                    case TrackAIElementShape.TriangleBottomLeft:
                        TrackDrawer.PaintBottomSide(g, this.aiAreaBrushes[speed][2], aiArea, target);
                        TrackDrawer.PaintLeftSide(g, this.aiAreaBrushes[speed][1], aiArea, target);
                        TrackDrawer.PaintTriangleDiagonalSide(g, this.aiAreaBrushes[speed][0], points, target);
                        break;
                }
            }
        }

        private static void PaintTopSide(Graphics g, Brush brush, Rectangle aiArea, Point target)
        {
            if (target.Y > aiArea.Top)
            {
                Point[] side =
                {
                    new Point(aiArea.Left, aiArea.Top),
                    new Point(aiArea.Right, aiArea.Top),
                    target
                };

                g.FillPolygon(brush, side);
            }
        }

        private static void PaintRightSide(Graphics g, Brush brush, Rectangle aiArea, Point target)
        {
            if (target.X < aiArea.Right)
            {
                Point[] side =
                {
                    new Point(aiArea.Right, aiArea.Top),
                    new Point(aiArea.Right, aiArea.Bottom),
                    target
                };

                g.FillPolygon(brush, side);
            }
        }

        private static void PaintBottomSide(Graphics g, Brush brush, Rectangle aiArea, Point target)
        {
            if (target.Y < aiArea.Bottom)
            {
                Point[] side =
                {
                    new Point(aiArea.Right, aiArea.Bottom),
                    new Point(aiArea.Left, aiArea.Bottom),
                    target
                };

                g.FillPolygon(brush, side);
            }
        }

        private static void PaintLeftSide(Graphics g, Brush brush, Rectangle aiArea, Point target)
        {
            if (target.X > aiArea.Left)
            {
                Point[] side =
                {
                    new Point(aiArea.Left, aiArea.Bottom),
                    new Point(aiArea.Left, aiArea.Top),
                    target
                };

                g.FillPolygon(brush, side);
            }
        }

        private static void PaintTriangleDiagonalSide(Graphics g, Brush brush, Point[] points, Point target)
        {
            points[points.Length - 2] = target;
            g.FillPolygon(brush, points);
        }

        private void HighlightHoveredAIElement(Graphics g, TrackAIElement aiElem, bool isAITargetHovered)
        {
            if (aiElem == null)
            {
                return;
            }

            Rectangle aiArea = TrackDrawer.GetAIAreaRectangle(aiElem);

            if (isAITargetHovered)
            {
                TrackDrawer.DrawAITargetLines(g, aiElem, aiArea, this.aiElementHighlightPen);
            }
            else
            {
                if (aiElem.AreaShape == TrackAIElementShape.Rectangle)
                {
                    g.DrawRectangle(this.aiElementHighlightPen, aiArea);
                }
                else
                {
                    Point[] points = TrackDrawer.GetAIAreaTriangle(aiElem);

                    g.DrawLines(this.aiElementHighlightPen, points);
                }
            }

            Color color = this.aiAreaBrushes[aiElem.Speed][0].Color;
            using (Brush brush = new LinearGradientBrush(new Point(0, aiArea.Top),
                                                         new Point(0, aiArea.Bottom),
                                                         Color.FromArgb(color.A / 2, color),
                                                         Color.Transparent))
            {
                this.HighlightAIElement(g, brush, aiElem, aiArea);
            }
        }

        private void HighlightSelectedAIElement(Graphics g, TrackAIElement aiElem)
        {
            if (aiElem == null)
            {
                return;
            }

            Rectangle aiArea = TrackDrawer.GetAIAreaRectangle(aiElem);
            TrackDrawer.DrawAITargetLines(g, aiElem, aiArea, this.aiElementSelectPen);
            this.HighlightAIElement(g, this.aiAreaBrushes[aiElem.Speed][0], aiElem, aiArea);
        }

        private void HighlightAIElement(Graphics g, Brush brush, TrackAIElement aiElem, Rectangle aiArea)
        {
            if (aiElem.AreaShape == TrackAIElementShape.Rectangle)
            {
                g.DrawRectangle(this.aiElementSelectPen, aiArea);
                g.FillRectangle(brush, aiArea);
            }
            else
            {
                Point[] points = TrackDrawer.GetAIAreaTriangle(aiElem);

                g.DrawPolygon(this.aiElementSelectPen, points);
                g.FillPolygon(brush, points);
            }
        }

        private static Rectangle GetAIAreaRectangle(TrackAIElement aiElem)
        {
            int aiAreaX = aiElem.Area.X * Tile.Size;
            int aiAreaY = aiElem.Area.Y * Tile.Size;
            int aiAreaWidth = aiElem.Area.Width * Tile.Size;
            int aiAreaHeight = aiElem.Area.Height * Tile.Size;

            return new Rectangle(aiAreaX, aiAreaY, aiAreaWidth - 1, aiAreaHeight - 1);
        }

        private static Point[] GetAIAreaTriangle(TrackAIElement aiElem)
        {
            Point[] points = aiElem.GetTriangle();

            int xCorrectionStep;
            int yCorrectionStep;

            switch (aiElem.AreaShape)
            {
                default:
                case TrackAIElementShape.TriangleTopLeft:
                    xCorrectionStep = 1;
                    yCorrectionStep = 1;
                    break;

                case TrackAIElementShape.TriangleTopRight:
                    xCorrectionStep = 0;
                    yCorrectionStep = 1;
                    break;

                case TrackAIElementShape.TriangleBottomRight:
                    xCorrectionStep = 0;
                    yCorrectionStep = 0;
                    break;

                case TrackAIElementShape.TriangleBottomLeft:
                    xCorrectionStep = 1;
                    yCorrectionStep = 0;
                    break;
            }

            for (int i = 0; i < points.Length; i++)
            {
                int xCorrection = points[i].X == aiElem.Area.Left ?
                    0 : xCorrectionStep;

                int yCorrection = points[i].Y == aiElem.Area.Top ?
                    0 : yCorrectionStep;

                points[i] =
                    new Point(points[i].X * Tile.Size - xCorrection,
                              points[i].Y * Tile.Size - yCorrection);
            }

            switch (aiElem.AreaShape)
            {
                case TrackAIElementShape.TriangleTopRight:
                    points[0].X--;
                    points[points.Length - 2].X--;
                    points[points.Length - 1].X--;
                    break;

                case TrackAIElementShape.TriangleBottomRight:
                    points[0].X--;
                    points[points.Length - 3].Y--;
                    points[points.Length - 2].Offset(-1, -1);
                    points[points.Length - 1].X--;
                    break;

                case TrackAIElementShape.TriangleBottomLeft:
                    points[points.Length - 3].Y--;
                    points[points.Length - 2].Y--;
                    break;
            }

            return points;
        }

        private static void DrawAITargetLines(Graphics g, TrackAIElement aiElem, Rectangle aiArea, Pen pen)
        {
            int pointX = aiElem.Target.X * Tile.Size;
            int pointY = aiElem.Target.Y * Tile.Size;

            Point targetPoint = new Point(pointX + Tile.Size / 2, pointY + Tile.Size / 2);

            switch (aiElem.AreaShape)
            {
                case TrackAIElementShape.Rectangle:
                    {
                        Point[] targetLines =
                        {
                            targetPoint,
                            new Point(aiArea.Left, aiArea.Top),
                            targetPoint,
                            new Point(aiArea.Right, aiArea.Top),
                            targetPoint,
                            new Point(aiArea.Right, aiArea.Bottom),
                            targetPoint,
                            new Point(aiArea.Left, aiArea.Bottom)
                        };
                        g.DrawLines(pen, targetLines);
                        break;
                    }

                case TrackAIElementShape.TriangleTopLeft:
                    {
                        Point[] targetLines =
                        {
                            targetPoint,
                            new Point(aiArea.Left, aiArea.Top),
                            targetPoint,
                            new Point(aiArea.Right, aiArea.Top),
                            targetPoint,
                            new Point(aiArea.Left, aiArea.Bottom)
                        };
                        g.DrawLines(pen, targetLines);
                        break;
                    }

                case TrackAIElementShape.TriangleTopRight:
                    {
                        Point[] targetLines =
                        {
                            targetPoint,
                            new Point(aiArea.Left, aiArea.Top),
                            targetPoint,
                            new Point(aiArea.Right, aiArea.Top),
                            targetPoint,
                            new Point(aiArea.Right, aiArea.Bottom)
                        };
                        g.DrawLines(pen, targetLines);
                        break;
                    }

                case TrackAIElementShape.TriangleBottomRight:
                    {
                        Point[] targetLines =
                        {
                            targetPoint,
                            new Point(aiArea.Right, aiArea.Top),
                            targetPoint,
                            new Point(aiArea.Right, aiArea.Bottom),
                            targetPoint,
                            new Point(aiArea.Left, aiArea.Bottom)
                        };
                        g.DrawLines(pen, targetLines);
                        break;
                    }

                case TrackAIElementShape.TriangleBottomLeft:
                    {
                        Point[] targetLines =
                        {
                            targetPoint,
                            new Point(aiArea.Left, aiArea.Top),
                            targetPoint,
                            new Point(aiArea.Right, aiArea.Bottom),
                            targetPoint,
                            new Point(aiArea.Left, aiArea.Bottom)
                        };
                        g.DrawLines(pen, targetLines);
                        break;
                    }
            }
        }

        private void PaintTrackOutbounds(PaintEventArgs e)
        {
            int mapWidth = (int)((this.track.Map.Width - this.scrollPosition.X) * Tile.Size * this.zoom);
            int mapHeight = (int)((this.track.Map.Height - this.scrollPosition.Y) * Tile.Size * this.zoom);

            if (e.ClipRectangle.Right > mapWidth || e.ClipRectangle.Bottom > mapHeight)
            {
                int x = Math.Max(mapWidth, e.ClipRectangle.Left);
                int y = Math.Max(mapHeight, e.ClipRectangle.Top);
                int width = e.ClipRectangle.Right - x;
                int height = e.ClipRectangle.Bottom - y;

                Rectangle[] outbounds = new Rectangle[]
                {
                    // Right outbound
                    new Rectangle(x, e.ClipRectangle.Y, width, e.ClipRectangle.Height),

                    // Bottom outbound
                    new Rectangle(e.ClipRectangle.X, y, e.ClipRectangle.Width, height)
                };

                e.Graphics.FillRectangles(Brushes.Black, outbounds);
            }
        }

        public void Dispose()
        {
            this.trackCache.Dispose();
            this.tileClipboardCache.Dispose();
            this.objectAreasCache.Dispose();

            this.zoomMatrix.Dispose();

            this.tileHighlightPen.Dispose();
            this.tileSelectPen.Dispose();
            this.tileSelectBrush.Dispose();

            this.overlayHighlightBrush.Dispose();

            this.lapLinePen.Dispose();
            this.lapLineOutlinePen.Dispose();
            this.arrowBrush.Dispose();
            this.arrowPen.Dispose();

            this.objectOutlinePen.Dispose();
            foreach (SolidBrush objectBrush in this.objectBrushes)
            {
                objectBrush.Dispose();
            }

            foreach (SolidBrush[] aiAreaBrushes in this.aiAreaBrushes)
            {
                foreach (SolidBrush aiAreaBrush in aiAreaBrushes)
                {
                    aiAreaBrush.Dispose();
                }
            }
            foreach (Pen aiAreaPen in this.aiAreaPens)
            {
                aiAreaPen.Dispose();
            }
            this.aiElementHighlightPen.Dispose();
            this.aiElementSelectPen.Dispose();

            this.translucidImageAttr.Dispose();
            this.grayScaleImageAttr.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
