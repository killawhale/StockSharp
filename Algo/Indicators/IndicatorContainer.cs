namespace StockSharp.Algo.Indicators
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.Localization;

	/// <summary>
	/// The container, storing indicators data.
	/// </summary>
	public class IndicatorContainer : IIndicatorContainer
	{
		private readonly FixedSynchronizedList<Tuple<IIndicatorValue, IIndicatorValue>> _values = new FixedSynchronizedList<Tuple<IIndicatorValue, IIndicatorValue>>();

		/// <summary>
		/// The maximal number of indicators values.
		/// </summary>
		public int MaxValueCount
		{
			get { return _values.BufferSize; }
			set { _values.BufferSize = value; }
		}

		/// <summary>
		/// The current number of saved values.
		/// </summary>
		public int Count
		{
			get { return _values.Count; }
		}

		/// <summary>
		/// Add new values.
		/// </summary>
		/// <param name="input">The input value of the indicator.</param>
		/// <param name="result">The resulting value of the indicator.</param>
		public virtual void AddValue(IIndicatorValue input, IIndicatorValue result)
		{
			_values.Add(Tuple.Create(input, result));
		}

		/// <summary>
		/// To get all values of the identifier.
		/// </summary>
		/// <returns>All values of the identifier. The empty set, if there are no values.</returns>
		public virtual IEnumerable<Tuple<IIndicatorValue, IIndicatorValue>> GetValues()
		{
			return _values.SyncGet(c => c.Reverse().ToArray());
		}

		/// <summary>
		/// To get the indicator value by the index.
		/// </summary>
		/// <param name="index">The sequential number of value from the end.</param>
		/// <returns>Input and resulting values of the indicator.</returns>
		public virtual Tuple<IIndicatorValue, IIndicatorValue> GetValue(int index)
		{
			if (index < 0)
				throw new ArgumentOutOfRangeException("index", index, LocalizedStrings.Str912);

			lock (_values.SyncRoot)
			{
				if (index >= _values.Count)
					throw new ArgumentOutOfRangeException("index", index, LocalizedStrings.Str913);

				return _values[_values.Count - 1 - index];
			}
		}

		/// <summary>
		/// To delete all values of the indicator.
		/// </summary>
		public virtual void ClearValues()
		{
			_values.Clear();
		}
	}
}