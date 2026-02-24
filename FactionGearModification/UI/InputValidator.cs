using System;
using System.Text.RegularExpressions;
using Verse;

namespace FactionGearCustomizer.UI
{
    public static class InputValidator
    {
        public static bool IsValidName(string name, out string errorMessage)
        {
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(name))
            {
                errorMessage = LanguageManager.Get("NameCannotBeEmpty");
                return false;
            }

            string trimmed = name.Trim();

            if (trimmed.Length == 0)
            {
                errorMessage = LanguageManager.Get("NameCannotBeEmpty");
                return false;
            }

            if (trimmed.Length < 1)
            {
                errorMessage = LanguageManager.Get("NameTooShort");
                return false;
            }

            if (trimmed.Length > 100)
            {
                errorMessage = LanguageManager.Get("NameTooLong");
                return false;
            }

            if (ContainsOnlySpecialChars(trimmed))
            {
                errorMessage = LanguageManager.Get("NameCannotBeOnlySpecialChars");
                return false;
            }

            return true;
        }

        public static string SanitizeName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            string sanitized = name.Trim();

            sanitized = Regex.Replace(sanitized, @"[\r\n\t]", "");

            while (sanitized.Contains("  "))
            {
                sanitized = sanitized.Replace("  ", " ");
            }

            return sanitized;
        }

        private static bool ContainsOnlySpecialChars(string str)
        {
            foreach (char c in str)
            {
                if (char.IsLetterOrDigit(c))
                    return false;
            }
            return true;
        }
    }
}
