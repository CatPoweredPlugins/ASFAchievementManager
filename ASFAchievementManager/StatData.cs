using System;

namespace ASFAchievementManager;

public enum StatValueKind {
	Numeric,
	Floating
}

public class StatData {
	public uint StatNum { get; set; }
	public int BitNum { get; set; }
	public bool IsSet { get; set; }
	public bool Restricted { get; set; }
	public uint Dependancy { get; set; }
	public uint DependancyValue { get; set; }
	public string? DependancyName { get; set; }
	public string? Name { get; set; }
	public StatValueKind ValueKind { get; set; }

	private uint? _numericValue;
	private float? _decimalValue;

	public uint NumericValue {
		get {
			if (ValueKind != StatValueKind.Numeric || _numericValue is null)
				throw new InvalidOperationException(
					$"Stat {StatNum} does not contain a numeric value."
				);

			return _numericValue.Value;
		}
		set {
			if (ValueKind != StatValueKind.Numeric)
				throw new InvalidOperationException(
					$"Stat {StatNum} is not a numeric stat."
				);

			_numericValue = value;
			_decimalValue = null;
		}
	}

	public float DecimalValue {
		get {
			if (ValueKind != StatValueKind.Floating || _decimalValue is null)
				throw new InvalidOperationException(
					$"Stat {StatNum} does not contain a decimal value."
				);

			return _decimalValue.Value;
		}
		set {
			if (ValueKind != StatValueKind.Floating)
				throw new InvalidOperationException(
					$"Stat {StatNum} is not a decimal stat."
				);

			_decimalValue = value;
			_numericValue = null;
		}
	}

	public uint RawValue {
		get {
			return ValueKind switch {
				StatValueKind.Numeric => _numericValue ?? 0,
				StatValueKind.Floating => BitConverter
					.SingleToUInt32Bits(_decimalValue ?? 0f),
				_ => 0
			};
		}
	}

	public static StatData FromRawValue(
		uint statNum,
		uint rawValue,
		StatValueKind kind
	) {
		var stat = new StatData {
			StatNum = statNum,
			ValueKind = kind
		};

		switch (kind) {
			case StatValueKind.Numeric:
				stat.NumericValue = rawValue;
				break;

			case StatValueKind.Floating:
				stat.DecimalValue =
					BitConverter.UInt32BitsToSingle(rawValue);
				break;
		}

		return stat;
	}
}
