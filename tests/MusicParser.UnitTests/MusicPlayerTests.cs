// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using NUnit;
using NUnit.Framework;

namespace MusicParser.UnitTests
{
    /// <summary>Contains tests for the <see cref="MusicPlayer" />.</summary>
    public static class MusicPlayerTests
    {
        /// <summary>Plays all supported notes.</summary>
        [TestCase]
        public static void PlayNotes()
        {
            var musicPlayer = new MusicPlayer();

            for (int i = 0; i < 85; i++)
            {
                musicPlayer.Play($@"
                    T120 O3 MN
                    N{i}
                ");
            }
        }

        /// <summary>Plays all supported notes.</summary>
        [TestCase]
        public static void PlayOctaves()
        {
            var musicPlayer = new MusicPlayer();

            for (int i = 0; i < 7; i++)
            {
                musicPlayer.Play($@"
                    T120 O3 MN
                    O{i} A- A A#
                         B- B B#
                         C- C C#
                         D- D D#
                         E- E E#
                         F- F F#
                         G- G G#
                ");
            }
        }

        /// <summary>Plays Jingle Bells</summary>
        [TestCase]
        public static void PlayJingleBells()
        {
            var musicPlayer = new MusicPlayer();
            musicPlayer.Play(@"
                T200 O3 MN
                L4 E E
                L2 E
                L4 E E
                L2 E
                L4 E G
                L3 C
                L8 D
                L1 E
                L4 F F
                L3 F
                L8 F
                L4 F E
                L2 E
                L8 E E
                L4 E D D E
                L2 D G
                L4 E E
                L2 E
                L4 E E
                L2 E
                L4 E G
                L3 C
                L8 D
                L1 E
                L4 F F
                L3 F
                L8 F
                L4 F E
                L2 E
                L8 E F
                L4 G G F D
                L2 C
            ");
        }
    }
}
