// Copyright 2009 The Noda Time Authors. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.Text;
using NodaTime.TimeZones;
using NodaTime.Utility;

namespace NodaTime.TzdbCompiler.Tzdb
{
    /// <summary>
    /// Defines one time zone rule with a validity range.
    /// </summary>
    /// <remarks>
    /// Immutable, threadsafe.
    /// </remarks>
    internal class ZoneRule : IEquatable<ZoneRule>
    {
        /// <summary>
        /// The string to replace "%s" with (if any) when formatting a daylight saving recurrence.
        /// </summary>
        private readonly string daylightSavingsIndicator;

        /// <summary>
        /// The recurrence pattern for the rule.
        /// </summary>
        private readonly ZoneRecurrence recurrence;

        /// <summary>
        /// Returns the name of the rule set this rule belongs to.
        /// </summary>
        public string Name => recurrence.Name;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZoneRule" /> class.
        /// </summary>
        /// <param name="recurrence">The recurrence definition of this rule.</param>
        /// <param name="daylightSavingsIndicator">The daylight savings indicator letter for time zone names.</param>
        public ZoneRule(ZoneRecurrence recurrence, string daylightSavingsIndicator)
        {
            this.recurrence = recurrence;
            this.daylightSavingsIndicator = daylightSavingsIndicator;
        }
       
        #region IEquatable<ZoneRule> Members
        /// <summary>
        ///   Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        ///   true if the current object is equal to the <paramref name = "other" /> parameter;
        ///   otherwise, false.
        /// </returns>
        public bool Equals(ZoneRule other) => other != null && Equals(recurrence, other.recurrence) && Equals(daylightSavingsIndicator, other.daylightSavingsIndicator);
        #endregion

        #region Operator overloads
        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(ZoneRule left, ZoneRule right) =>        
            ReferenceEquals(left, null) ? ReferenceEquals(right, null) : left.Equals(right);

        /// <summary>
        ///   Implements the operator !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(ZoneRule left, ZoneRule right) => !(left == right);
        #endregion

        /// <summary>
        /// Retrieves the recurrence, after applying the specified name format.
        /// </summary>
        /// <remarks>
        /// Multiple zones may apply the same set of rules as to when they change into/out of
        /// daylight saving time, but with different names.
        /// </remarks>
        /// <param name="nameFormat">The name format.</param>
        public ZoneRecurrence GetRecurrence(String nameFormat)
        {
            return recurrence.WithName(FormatName(nameFormat));
        }

        private string FormatName(string nameFormat)
        {
            Preconditions.CheckNotNull(nameFormat, "nameFormat");
            int index = nameFormat.IndexOf("/", StringComparison.Ordinal);
            if (index > 0)
            {
                return recurrence.Savings == Offset.Zero ? nameFormat.Substring(0, index) : nameFormat.Substring(index + 1);
            }
            index = nameFormat.IndexOf("%s", StringComparison.Ordinal);
            if (index < 0)
            {
                return nameFormat;
            }
            var left = nameFormat.Substring(0, index);
            var right = nameFormat.Substring(index + 2);
            return left + daylightSavingsIndicator + right;
        }

        #region Object overrides
        /// <summary>
        /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance;
        /// otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj) => Equals(obj as ZoneRule);

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data
        /// structures like a hash table. 
        /// </returns>
        public override int GetHashCode() => HashCodeHelper.Hash(recurrence, daylightSavingsIndicator);

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append(recurrence);
            if (daylightSavingsIndicator != null)
            {
                builder.Append(" \"").Append(daylightSavingsIndicator).Append("\"");
            }
            return builder.ToString();
        }
        #endregion Object overrides
    }
}
