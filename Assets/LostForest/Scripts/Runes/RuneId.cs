using System;
using UnityEngine;

namespace LostForest.Phase2.Runes
{
    public sealed class RuneId : MonoBehaviour
    {
        public const char NoRune = '\0';
        public const char OwlFeatherSymbol = '#';

        [SerializeField] private string runeLetter = "A";

        public char Letter => Normalize(runeLetter);
        public bool IsOwlFeather => Letter == OwlFeatherSymbol;
        public string LetterText => IsValidMarkerSymbol(Letter) ? Letter.ToString() : string.Empty;

        public void SetRune(char newRuneLetter)
        {
            char normalized = Normalize(newRuneLetter);
            runeLetter = IsValidMarkerSymbol(normalized) ? normalized.ToString() : string.Empty;
        }

        public static bool IsValidRune(char runeLetter)
        {
            return runeLetter >= 'A' && runeLetter <= 'Z';
        }

        public static bool IsValidMarkerSymbol(char markerSymbol)
        {
            return IsValidRune(markerSymbol) || markerSymbol == OwlFeatherSymbol;
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
