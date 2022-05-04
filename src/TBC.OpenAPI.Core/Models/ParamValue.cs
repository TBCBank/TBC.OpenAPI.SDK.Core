using System.Globalization;
using System.Runtime.CompilerServices;

namespace TBC.OpenAPI.Core.Models
{
	public sealed class ParamValue
	{
		internal readonly string? Value;

		internal readonly IEnumerable<ParamValue>? Values;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private ParamValue(string? value) => Value = value;

		private ParamValue(IEnumerable<ParamValue>? values) => Values = values;

		public override string ToString() => Value ?? string.Empty;

#pragma warning disable CA2225 // Operator overloads have named alternates

		public static implicit operator ParamValue(bool value) => new(value ? "true" : "false");

		public static implicit operator ParamValue(bool? value) => new(value.HasValue ? value.Value ? "true" : "false" : null);

        public static implicit operator ParamValue(byte value) => new(value.ToString(CultureInfo.InvariantCulture));


        public static implicit operator ParamValue(byte? value) => new(value?.ToString(CultureInfo.InvariantCulture));


        public static implicit operator ParamValue(sbyte value) => new(value.ToString(CultureInfo.InvariantCulture));

		public static implicit operator ParamValue(sbyte? value) => new(value?.ToString(CultureInfo.InvariantCulture));

        public static implicit operator ParamValue(short value) => new(value.ToString(CultureInfo.InvariantCulture));

		public static implicit operator ParamValue(short? value) => new(value?.ToString(CultureInfo.InvariantCulture));

        public static implicit operator ParamValue(ushort value) => new(value.ToString(CultureInfo.InvariantCulture));

		public static implicit operator ParamValue(ushort? value) => new(value?.ToString(CultureInfo.InvariantCulture));

        public static implicit operator ParamValue(int value) => new(value.ToString(CultureInfo.InvariantCulture));

		public static implicit operator ParamValue(int? value) => new(value?.ToString(CultureInfo.InvariantCulture));

        public static implicit operator ParamValue(uint value) => new(value.ToString(CultureInfo.InvariantCulture));

		public static implicit operator ParamValue(uint? value) => new(value?.ToString(CultureInfo.InvariantCulture));

        public static implicit operator ParamValue(long value) => new(value.ToString(CultureInfo.InvariantCulture));

		public static implicit operator ParamValue(long? value) => new(value?.ToString(CultureInfo.InvariantCulture));

        public static implicit operator ParamValue(ulong value) => new(value.ToString(CultureInfo.InvariantCulture));

		public static implicit operator ParamValue(ulong? value) => new(value?.ToString(CultureInfo.InvariantCulture));

        public static implicit operator ParamValue(decimal value) => new(value.ToString(CultureInfo.InvariantCulture));

		public static implicit operator ParamValue(decimal? value) => new(value?.ToString(CultureInfo.InvariantCulture));

        public static implicit operator ParamValue(float value) => new(value.ToString(CultureInfo.InvariantCulture));

		public static implicit operator ParamValue(float? value) => new(value?.ToString(CultureInfo.InvariantCulture));

        public static implicit operator ParamValue(double value) => new(value.ToString(CultureInfo.InvariantCulture));

		public static implicit operator ParamValue(double? value) => new(value?.ToString(CultureInfo.InvariantCulture));

		public static implicit operator ParamValue(string value) => new(value);

		public static implicit operator ParamValue(DateTime value) => new(value.ToString("s"));

		public static implicit operator ParamValue(DateTime? value) => new(value?.ToString("s"));

#if NET6_0_OR_GREATER
		public static implicit operator ParamValue(DateOnly value) => new(value.ToString("yyyy-MM-dd"));

		public static implicit operator ParamValue(DateOnly? value) => new(value?.ToString("yyyy-MM-dd"));

		public static implicit operator ParamValue(TimeOnly value) => new(value.ToString("HH:mm:ss"));

		public static implicit operator ParamValue(TimeOnly? value) => new(value?.ToString("HH:mm:ss"));
#endif

		public static implicit operator ParamValue(Guid value) => new(value.ToString());

        public static implicit operator ParamValue(Guid? value) => new(value?.ToString());

		public static implicit operator ParamValue(Enum? value) => new(value?.ToString());

		public static implicit operator ParamValue(List<string>? value) => new(value?.ConvertAll(a => (ParamValue)a));

		public static implicit operator ParamValue(List<int>? value) => new(value?.ConvertAll(a => (ParamValue)a));

		public static implicit operator ParamValue(List<long>? value) => new(value?.ConvertAll(a => (ParamValue)a));

		public static implicit operator ParamValue(List<DateTime>? value) => new(value?.ConvertAll(a => (ParamValue)a));

#if NET6_0_OR_GREATER
		public static implicit operator ParamValue(List<DateOnly>? value) => new(value?.ConvertAll(a => (ParamValue)a));
		public static implicit operator ParamValue(List<TimeOnly>? value) => new(value?.ConvertAll(a => (ParamValue)a));
#endif

		public static implicit operator ParamValue(List<Guid>? value) => new(value?.ConvertAll(a => (ParamValue)a));

		public static implicit operator ParamValue(List<Enum>? value) => new(value?.ConvertAll(a => (ParamValue)a));

		public static implicit operator ParamValue(Dictionary<string, string> value) => new(GetKeyValueString(value));

#pragma warning restore CA2225 // Operator overloads have named alternates

		private static string GetKeyValueString(Dictionary<string, string> dictionary) =>
			string.Join("&", dictionary.Select(o => $"{o.Key}={o.Value}"));
	}
}
