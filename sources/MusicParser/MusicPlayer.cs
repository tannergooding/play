// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Threading;

namespace MusicParser
{
    /// <summary>Represents a music player.</summary>
    public class MusicPlayer
    {
        private static readonly uint[] NoteOffset = new uint[7] {
             1 - 0, // 'A'
             3 - 1, // 'B'
             4 - 2, // 'C'
             6 - 3, // 'D'
             8 - 4, // 'E'
             9 - 5, // 'F'
            11 - 6, // 'G'
        };

        /// <summary>Parses a string into a series of notes and plays them.</summary>
        /// <param name="expression">The expression to parse.</param>
        public void Play(string expression)
        {
            const uint Pause = 0;

            const double StaccatoTempo = 3.0 / 4.0;
            const double NormalTempo = 7.0 / 8.0;
            const double LegatoTempo = 1.0;

            uint octave = 3, tempo = 120, noteLength = 4;
            double tempoModifier = NormalTempo;

            for (int i = 0; i < expression.Length; i++)
            {
                char c = GetCharacter(i);

                switch (c)
                {
                    case '\t':
                    case '\n':
                    case '\v':
                    case '\f':
                    case '\r':
                    case ' ':
                    {
                        // We ignore whitespace
                        break;
                    }

                    case 'O':
                    {
                        octave = ParseOctave(ref i);
                        break;
                    }

                    case '<':
                    {
                        octave--;

                        if (octave > 6)
                        {
                            ThrowException("Octave cannot go below 0.", i);
                        }
                        break;
                    }

                    case '>':
                    {
                        octave++;

                        if (octave > 6)
                        {
                            ThrowException("Octave cannot go above 6.", i);
                        }
                        break;
                    }

                    case 'A':
                    case 'B':
                    case 'C':
                    case 'D':
                    case 'E':
                    case 'F':
                    case 'G':
                    {
                        // Here we are basically taking the current octave, the note given, and any modifiers
                        // to produce a MIDI note. We can then easily translate that into a frequency to play.

                        uint note = (uint)(c - 'A');
                        note += (octave * 12) + NoteOffset[note];

                        char noteModifier = PeekNextCharacter(i);

                        if ((noteModifier == '+') || (noteModifier == '#'))
                        {
                            // We don't have a B# or E# available in the MIDI scale we are using

                            if ((c == 'A') || (c == 'C') || (c == 'D') || (c == 'F') || (c == 'G'))
                            {
                                note++;
                            }
                            i++;
                        }
                        else if (noteModifier == '-')
                        {
                            // We don't have a Cb or Fb available in the MIDI scale we are using

                            if ((c == 'A') || (c == 'B') || (c == 'D') || (c == 'E') || (c == 'G'))
                            {
                                note--;
                            }
                            i++;
                        }

                        uint previousNoteLength = noteLength;
                        noteModifier = PeekNextCharacter(i);

                        if ((noteModifier >= '0') && (noteModifier <= '9'))
                        {
                            noteLength = ParseNoteLength(ref i);
                        }
                        double noteLengthModifier = ParseNoteLengthModifier(ref i);

                        PlayNote(note, noteLengthModifier);
                        noteLength = previousNoteLength;
                        break;
                    }

                    case 'N':
                    {
                        // Here we interpret the note given directly as a MIDI number. However, we
                        // also add 5 to account for the lowest notes that are considered invalid.

                        uint note = ParseNote(ref i) + 5;
                        double noteLengthModifier = ParseNoteLengthModifier(ref i);

                        PlayNote(note, noteLengthModifier);
                        break;
                    }

                    case 'L':
                    {
                        noteLength = ParseNoteLength(ref i);
                        break;
                    }

                    case 'M':
                    {
                        c = GetNextCharacter(ref i);

                        if (c == 'B')
                        {
                            // We don't support background execution right now
                        }
                        else if (c == 'F')
                        {
                            // We don't support foreground execution right now
                        }
                        else if (c == 'L')
                        {
                            tempoModifier = LegatoTempo;
                        }
                        else if (c == 'N')
                        {
                            tempoModifier = NormalTempo;
                        }
                        else if (c == 'S')
                        {
                            tempoModifier = StaccatoTempo;
                        }
                        else
                        {
                            goto default;
                        }
                        break;
                    }

                    case 'P':
                    {
                        uint previousNoteLength = noteLength;
                        noteLength = ParseNoteLength(ref i);

                        double noteLengthModifier = ParseNoteLengthModifier(ref i);

                        PlayNote(Pause, noteLengthModifier);
                        noteLength = previousNoteLength;
                        break;
                    }

                    case 'T':
                    {
                        tempo = ParseTempo(ref i);
                        break;
                    }

                    default:
                    {
                        ThrowException("Unexpected character", i);
                        break;
                    }
                }
            }

            double ComputeDuration(double noteLengthModifier)
            {
                // The tempo is the number of quarter-notes per minute and we need to convert that to milliseconds.

                double value = (60.0 / tempo) * 1000.0;     // Convert the quaternotes per minute into milliseconds per quarternote.
                value *= (4.0 / noteLength);                // Scale that by the number of quater-notes the note length represents
                value *= noteLengthModifier;                // Scale that by the note length modifier
                value *= tempoModifier;                     // Finally, scale that by the tempo modifier to ensure we play at the appropriate speed

                return value;
            }

            char GetNextCharacter(ref int i)
            {
                return GetCharacter(++i);
            }

            char GetCharacter(int i)
            {
                if (i >= expression.Length)
                {
                    ThrowException("Unexpected end of stream.", i);
                }

                return char.ToUpper(expression[i]);
            }

            char PeekNextCharacter(int i)
            {
                return GetCharacter(++i);
            }

            double ParseNoteLengthModifier(ref int i)
            {
                char c = PeekNextCharacter(i);
                uint dottedCount = 0;

                while (c == '.')
                {
                    dottedCount++;
                    c = PeekNextCharacter(++i);
                }

                switch (dottedCount)
                {
                    case 0:
                    {
                        return 1.0;
                    }

                    case 1:
                    {
                        return 1.5;
                    }

                    case 2:
                    {
                        return 1.75;
                    }

                    default:
                    {
                        ThrowException("The dotted count must be between 0 and 2 (inclusive).", i);
                        goto case 0;
                    }
                }
            }

            uint ParseNote(ref int i)
            {
                char c = GetNextCharacter(ref i);
                uint value = (uint)(c - '0');

                if (value > 9)
                {
                    ThrowException("Note must be between 0 and 84 (inclusive).", i);
                }

                c = PeekNextCharacter(i);

                if ((c >= '0') && (c <= '9'))
                {
                    value *= 10;
                    value += (uint)(c - '0');

                    i++;
                }

                if (value > 84)
                {
                    ThrowException("Note must be between 0 and 84 (inclusive).", i);
                }
                return value;
            }

            uint ParseNoteLength(ref int i)
            {
                char c = GetNextCharacter(ref i);
                uint value = (uint)(c - '0');

                if ((value < 1) || (value > 9))
                {
                    ThrowException("Note length must be between 1 and 64 (inclusive).", i);
                }

                c = PeekNextCharacter(i);

                if ((c >= '0') && (c <= '9'))
                {
                    value *= 10;
                    value += (uint)(c - '0');

                    i++;
                }

                if ((value < 1) || (value > 64))
                {
                    ThrowException("Note length must be between 1 and 64 (inclusive).", i);
                }
                return value;
            }

            uint ParseOctave(ref int i)
            {
                char c = GetNextCharacter(ref i);
                uint value = (uint)(c - '0');

                if (value > 6)
                {
                    ThrowException("Octave must be between 0 and 6 (inclusive).", i);
                }
                return value;
            }

            uint ParseTempo(ref int i)
            {
                char c = GetNextCharacter(ref i);
                uint value = (uint)(c - '0');

                c = PeekNextCharacter(i);

                if ((value < 1) || (value > 9) || (c < '0') || (c > '9'))
                {
                    ThrowException("Tempo must be between 32 and 255 (inclusive).", i);
                }

                value *= 10;
                value += (uint)(c - '0');

                i++;

                c = PeekNextCharacter(i);

                if ((c >= '0') && (c <= '9'))
                {
                    value *= 10;
                    value += (uint)(c - '0');

                    i++;
                }

                if ((value < 32) || (value > 255))
                {
                    ThrowException("Tempo must be between 32 and 255 (inclusive).", i);
                }

                return value;
            }

            void PlayNote(uint note, double noteLengthModifier)
            {
                double duration = ComputeDuration(noteLengthModifier);
                duration = Math.Round(duration);

                if (note < 6)
                {
                    Thread.Sleep((int)(duration));
                }
                else
                {
                    double frequency = Math.Pow(2.0, ((note - 49.0) / 12.0)) * 440.0;
                    frequency = Math.Round(frequency);
                    Sound((int)(frequency), (int)(duration));
                }

                noteLengthModifier = 0;
            }

            void ThrowException(string message, int i)
            {
                throw new ArgumentOutOfRangeException($"Invalid token at {i}: '{expression[i]}'. {message}");
            }
        }

        /// <summary>Makes a sound at the specified frequency and for the specified duration.</summary>
        /// <param name="frequency">The frequency of the sound to play, in hertz.</param>
        /// <param name="duration">The duration of the sound to play, in milliseconds.</param>
        public virtual void Sound(double frequency, double duration)
        {
            Console.Beep(
                (int)(Math.Round(frequency)),
                (int)(Math.Round(duration))
            );
        }
    }
}
