using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MMBot
{
    /// <summary>
    /// Credit to stack exchange team: https://github.com/opserver/Opserver
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Adds the parameter items to this list.
        /// </summary>
        public static void AddAll<T>(this List<T> list, params T[] items)
        {
            list.AddRange(items);
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var item in source)
            {
                action(item);
            }
        }

        /// <summary>
        /// Full representation of a number
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static string Full(this int number)
        {
            return String.Format("{0:#,##0}", number);
        }

        public static string GetDescription<T>(this T? enumerationValue) where T : struct
        {
            return enumerationValue.HasValue ? enumerationValue.Value.GetDescription() : string.Empty;
        }

        /// <summary>
        /// Gets the Description attribute text or the .ToString() of an enum member
        /// </summary>
        public static string GetDescription<T>(this T enumerationValue) where T : struct
        {
            var type = enumerationValue.GetType();
            if (!type.IsEnum) throw new ArgumentException("EnumerationValue must be of Enum type", "enumerationValue");
            var memberInfo = type.GetMember(enumerationValue.ToString());
            if (memberInfo.Length > 0)
            {
                var attrs = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (attrs.Length > 0)
                    return ((DescriptionAttribute)attrs[0]).Description;
            }
            return enumerationValue.ToString();
        }

        /// <summary>
        /// Returns a string with all the DBML-mapped property names and their values. Each tuple will be separated by 'joinSeparator'.
        /// </summary>
        public static string GetPropertyNamesAndValues(this object o, string joinSeparator = "\n")
        {
            if (o == null)
                return "";

            var props = o.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).AsEnumerable();

            var strings = props.Select(p => p.Name + ":" + p.GetValue(o, null));
            return string.Join(joinSeparator, strings);
        }

        /// <summary>
        /// Answers true if this String is neither null or empty.
        /// </summary>
        /// <remarks>I'm also tired of typing !String.IsNullOrEmpty(s)</remarks>
        public static bool HasValue(this string s)
        {
            return !string.IsNullOrEmpty(s);
        }

        /// <summary>
        /// Returns the default value if given a default(T)
        /// </summary>
        public static T IfDefaultReturn<T>(this T val, T dDefault) where T : struct
        {
            return val.Equals(default(T)) ? dDefault : val;
        }

        /// <summary>
        /// Answers true if this String is either null or empty.
        /// </summary>
        /// <remarks>I'm so tired of typing String.IsNullOrEmpty(s)</remarks>
        public static bool IsNullOrEmpty(this string s)
        {
            return string.IsNullOrEmpty(s);
        }

        /// <summary>
        /// Returns the first non-null/non-empty parameter when this String is null/empty.
        /// </summary>
        public static string IsNullOrEmptyReturn(this string s, params string[] otherPossibleResults)
        {
            if (s.HasValue())
                return s;

            if (otherPossibleResults == null)
                return "";

            foreach (var t in otherPossibleResults)
            {
                if (t.HasValue())
                    return t;
            }
            return "";
        }

        public static int LineCount(this string s)
        {
            var n = 0;
            foreach (var c in s)
            {
                if (c == '\n') n++;
            }
            return n + 1;
        }

        /// <summary>
        /// Micro representation of a number
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static string Micro(this int number)
        {
            if (number >= 1000000)
            {
                return String.Format("{0:0.0m}", (double)number / 1000000);
            }

            if (number >= 100000)
            {
                return String.Format("{0:0k}", (double)number / 1000);
            }

            if (number >= 10000)
            {
                return String.Format("{0:0k}", (double)number / 1000);
            }

            if (number >= 1000)
            {
                return String.Format("{0:0k}", (double)number / 1000);
            }

            return String.Format("{0:#,##0}", number);
        }

        /// <summary>
        /// Returns true when the next number between 1 and 100 is less than or equal to 'percentChanceToOccur'.
        /// </summary>
        public static bool PercentChance(this Random random, int percentChanceToOccur)
        {
            return random.Next(1, 100) <= percentChanceToOccur;
        }

        /// <summary>
        /// A brain dead pluralizer. 1.Pluralize("time") => "1 time"
        /// </summary>
        public static string Pluralize(this int number, string item, bool includeNumber = true)
        {
            var numString = includeNumber ? number.ToComma() + " " : "";
            return number == 1
                       ? numString + item
                       : numString + (item.EndsWith("y") ? item.Remove(item.Length - 1) + "ies" : item + "s");
        }

        /// <summary>
        /// A brain dead pluralizer. 1.Pluralize("time") => "1 time"
        /// </summary>
        public static string Pluralize(this long number, string item, bool includeNumber = true)
        {
            var numString = includeNumber ? number.ToComma() + " " : "";
            return number == 1
                       ? numString + item
                       : numString + (item.EndsWith("y") ? item.Remove(item.Length - 1) + "ies" : item + "s");
        }

        /// <summary>
        /// A plurailizer that accepts the count, single and plural variants of the text
        /// </summary>
        public static string Pluralize(this int number, string single, string plural, bool includeNumber = true)
        {
            var numString = includeNumber ? number.ToComma() + " " : "";
            return number == 1 ? numString + single : numString + plural;
        }

        /// <summary>
        /// Returns the pluralized version of 'noun' when required by 'number'.
        /// </summary>
        public static string Pluralize(this string noun, int number, string pluralForm = null)
        {
            return number == 1 ? noun : pluralForm.IsNullOrEmptyReturn((noun ?? "") + "s");
        }

        public static void Raise(this EventHandler handler, object sender, EventArgs e)
        {
            if (handler != null) handler(sender, e);
        }

        public static void Raise<T>(this EventHandler<T> handler, object sender, T e) where T : EventArgs
        {
            if (handler != null) handler(sender, e);
        }

        public static string ToComma(this int? number, string valueIfZero = null)
        {
            return number.HasValue ? ToComma(number.Value, valueIfZero) : "";
        }

        public static string ToComma(this int number, string valueIfZero = null)
        {
            if (number == 0 && valueIfZero != null) return valueIfZero;
            return string.Format("{0:n0}", number);
        }

        public static string ToComma(this long? number, string valueIfZero = null)
        {
            return number.HasValue ? ToComma(number.Value, valueIfZero) : "";
        }

        public static string ToComma(this long number, string valueIfZero = null)
        {
            if (number == 0 && valueIfZero != null) return valueIfZero;
            return string.Format("{0:n0}", number);
        }

        public static int ToSecondsFromDays(this int representingDays)
        {
            return representingDays * 24 * 60 * 60;
        }

        /// <summary>
        /// force string to be maxlen or smaller
        /// </summary>
        public static string Truncate(this string s, int maxLength)
        {
            if (s.IsNullOrEmpty()) return s;
            return (s.Length > maxLength) ? s.Remove(maxLength) : s;
        }

        public static string TruncateWithEllipsis(this string s, int maxLength)
        {
            if (s.IsNullOrEmpty()) return s;
            if (s.Length <= maxLength) return s;

            return string.Format("{0}...", Truncate(s, maxLength - 3));
        }

        public static async Task<JToken> ToJsonAsync(this string jsonString)
        {
            if (jsonString != null && jsonString.StartsWith("["))
            {
                return await JsonConvert.DeserializeObjectAsync<JArray>(jsonString);
            }
            return await JsonConvert.DeserializeObjectAsync<JObject>(jsonString);
        }

        public static JToken ToJson(this string jsonString)
        {
            return jsonString.ToJsonAsync().Result;
        }

        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default(TValue))
        {
            TValue val;
            if (!dictionary.TryGetValue(key, out val))
            {
                return defaultValue;
            }
            return val;
        }
    }
}
