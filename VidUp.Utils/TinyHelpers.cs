using System;

namespace Drexel.VidUp.Utils
{
    public class TinyHelpers
    {
        public static string QuotaExceededString = "VidUp's YouTube access is closed for today (API quota exceeded). Try again after midnight PT (UTC-8).";

        public static string TrimLineBreakAtEnd(string text)
        {
            if (text == null)
            {
                return null;
            }

            return text.TrimEnd('\r', '\n');
        }

        public static Predicate<T> PredicateOr<T>(params Predicate<T>[] predicates)
        {
            return delegate (T item)
            {
                foreach (Predicate<T> predicate in predicates)
                {
                    if (predicate(item))
                    {
                        return true;
                    }
                }
                return false;
            };
        }

        public static Predicate<T> PredicateAnd<T>(params Predicate<T>[] predicates)
        {
            return delegate (T item)
            {
                foreach (Predicate<T> predicate in predicates)
                {
                    if (!predicate(item))
                    {
                        return false;
                    }
                }
                return true;
            };
        }
    }
}
