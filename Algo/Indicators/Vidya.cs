namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;
	using System.Linq;
	using System;

	using StockSharp.Localization;

	/// <summary>
	/// The dynamic average of variable index  (Variable Index Dynamic Average).
	/// </summary>
	/// <remarks>
	/// http://www2.wealth-lab.com/WL5Wiki/Vidya.ashx http://www.mql5.com/en/code/75.
	/// </remarks>
	[DisplayName("Vidya")]
	[DescriptionLoc(LocalizedStrings.Str755Key)]
	public class Vidya : LengthIndicator<decimal>
	{
		private decimal _multiplier = 1;
		private decimal _prevFinalValue;

		private readonly ChandeMomentumOscillator _cmo;

		/// <summary>
		/// To create the indicator <see cref="Vidya"/>.
		/// </summary>
		public Vidya()
		{
			_cmo = new ChandeMomentumOscillator();
			Length = 15;
		}

		/// <summary>
		/// To reset the indicator status to initial. The method is called each time when initial settings are changed (for example, the length of period).
		/// </summary>
		public override void Reset()
		{
			_cmo.Length = Length;
			_multiplier = 2m / (Length + 1);
			_prevFinalValue = 0;

			base.Reset();
		}

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var newValue = input.GetValue<decimal>();

			// Вычисляем  СMO
			var cmoValue = _cmo.Process(input).GetValue<decimal>();

			// Вычисляем Vidya
			if (!IsFormed)
			{
				if (!input.IsFinal)
					return new DecimalIndicatorValue(this, ((Buffer.Skip(1).Sum() + newValue) / Length));

				Buffer.Add(newValue);

				_prevFinalValue = Buffer.Sum() / Length;

				return new DecimalIndicatorValue(this, _prevFinalValue);
			}

			var curValue = (newValue - _prevFinalValue) * _multiplier * Math.Abs(cmoValue / 100m) + _prevFinalValue;
				
			if (input.IsFinal)
				_prevFinalValue = curValue;

			return new DecimalIndicatorValue(this, curValue);
		}
	}
}