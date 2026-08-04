using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SmallWorld.Core
{
    /// <summary>
    /// Runtime logging boundary that redacts common personal identifiers and absolute paths.
    /// Exceptions are summarized without stack traces because editor stack traces contain paths.
    /// </summary>
    public static class SafeGameLogger
    {
        private const string Prefix = "[SmallWorld] ";
        private const string Redacted = "[redacted]";

        private static readonly Regex EmailPattern = new Regex(
            @"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex WindowsPathPattern = new Regex(
            @"(?<!\w)(?:[A-Z]:\\|\\\\)[^\r\n\t\""<>|]+",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex UnixPathPattern = new Regex(
            @"(?<!\w)/(?:[^/\s]+/)+[^\s]*",
            RegexOptions.CultureInvariant);

        public static void Info(string message)
        {
            Debug.Log(Prefix + Sanitize(message));
        }

        public static void Warning(string message)
        {
            Debug.LogWarning(Prefix + Sanitize(message));
        }

        public static void Error(string message)
        {
            Debug.LogError(Prefix + Sanitize(message));
        }

        public static void Error(string message, Exception exception)
        {
            string exceptionSummary = exception == null
                ? "Unknown error"
                : exception.GetType().Name + ": " + exception.Message;
            Debug.LogError(Prefix + Sanitize(message) + " (" + Sanitize(exceptionSummary) + ")");
        }

        public static string Sanitize(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return string.Empty;
            }

            string sanitized = EmailPattern.Replace(message, Redacted);
            sanitized = WindowsPathPattern.Replace(sanitized, Redacted);
            return UnixPathPattern.Replace(sanitized, Redacted);
        }
    }
}
