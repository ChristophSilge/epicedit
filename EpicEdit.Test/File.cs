#region GPL statement
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
using System;
using System.IO;

namespace EpicEdit.Test
{
    /// <summary>
    /// File manipulation class used for unit testing.
    /// </summary>
    internal static class File
    {
        private readonly static string RelativePath =
            ".." + Path.DirectorySeparatorChar +
            ".." + Path.DirectorySeparatorChar +
            "files" + Path.DirectorySeparatorChar;

        private static string GetRomFileName(Region region)
        {
            switch (region)
            {
                case Region.Jap:
                    return "Super Mario Kart (Japan).sfc";

                default:
                case Region.US:
                    return "Super Mario Kart (USA).sfc";

                case Region.Euro:
                    return "Super Mario Kart (Europe).sfc";
            }
        }

        public static Game GetGame(Region region)
        {
            return File.GetGame(File.GetRomFileName(region));
        }

        public static Game GetGame(string fileName)
        {
            return new Game(File.RelativePath + fileName);
        }

        public static byte[] ReadRom(Region region)
        {
            return File.ReadFile(File.GetRomFileName(region));
        }

        public static byte[] ReadFile(string fileName)
        {
            return System.IO.File.ReadAllBytes(File.RelativePath + fileName);
        }

        public static byte[] ReadBlock(byte[] buffer, int offset, int length)
        {
            byte[] bytes = new byte[length];
            Buffer.BlockCopy(buffer, offset, bytes, 0, length);
            return bytes;
        }
    }
}
