using System;
using UnityEngine;

namespace LostForest.Phase2.Runes
{
    public sealed class RuneId : MonoBehaviour
    {
        public const char NoRune = '\0';

        [SerializeField] private string runeLetter = "A";

        public char Letter => Normalize(runeLetter);
        public string LetterText => IsValidRune(Letter) ? Letter.ToString() : string.Empty;

        public void SetRune(char newRuneLetter)
        {
            char normalized = Normalize(newRuneLetter);
            runeLetter = IsValidRune(normalized) ? normalized.ToString() : string.Empty;
        }

        public static bool IsValidRune(char runeLetter)
        {
            return runeLetter >= 'A' && runeLetter <= 'Z';
        }

        public static char Normalize(char runeLetter)
        {
            return char.ToUpperInvariant(runeLetter);
        }

        private static char Normalize(string runeLetterText)
        {
            if (string.IsNullOrWhiteSpace(runeLetterText))
            {
                return NoRune;
            }

            return Normalize(runeLetterText.Trim()[0]);
        }
    }
}
