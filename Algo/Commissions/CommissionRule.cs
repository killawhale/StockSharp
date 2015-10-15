namespace StockSharp.Algo.Commissions
{
	using System;
	using System.ComponentModel;
	using System.Runtime.Serialization;
	using DataContract = System.Runtime.Serialization.DataContractAttribute;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The commission calculating rule.
	/// </summary>
	[DataContract]
	public abstract class CommissionRule : NotifiableObject, ICommissionRule
	{
		/// <summary>
		/// Initialize <see cref="CommissionRule"/>.
		/// </summary>
		protected CommissionRule()
		{
		}

		private Unit _value = new Unit();

		/// <summary>
		/// Commission value.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str159Key)]
		[DescriptionLoc(LocalizedStrings.CommissionValueKey)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public Unit Value
		{
			get { return _value; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_value = value;
				NotifyChanged("Value");
			}
		}

		/// <summary>
		/// Total commission.
		/// </summary>
		[Browsable(false)]
		public decimal Commission { get; private set; }

		private string _title;

		/// <summary>
		/// Header.
		/// </summary>
		[Browsable(false)]
		public string Title
		{
			get { return _title; }
			protected set
			{
				_title = value;
				NotifyChanged("Title");
			}
		}

		/// <summary>
		/// To reset the state.
		/// </summary>
		public virtual void Reset()
		{
			Commission = 0;
		}

		/// <summary>
		/// To calculate commission.
		/// </summary>
		/// <param name="message">The message containing the information about the order or own trade.</param>
		/// <returns>The commission. If the commission can not be calculated then <see langword="null" /> will be returned.</returns>
		public decimal? ProcessExecution(ExecutionMessage message)
		{
			var commission = OnProcessExecution(message);

			if (commission != null)
				Commission += commission.Value;

			return commission;
		}

		/// <summary>
		/// To calculate commission.
		/// </summary>
		/// <param name="message">The message containing the information about the order or own trade.</param>
		/// <returns>The commission. If the commission can not be calculated then <see langword="null" /> will be returned.</returns>
		protected abstract decimal? OnProcessExecution(ExecutionMessage message);

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public virtual void Load(SettingsStorage storage)
		{
			Value = storage.GetValue<Unit>("Value");
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public virtual void Save(SettingsStorage storage)
		{
			storage.SetValue("Value", Value);
		}
	}

	/// <summary>
	/// Order commission.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str504Key)]
	[DescriptionLoc(LocalizedStrings.Str660Key)]
	public class CommissionPerOrderRule : CommissionRule
	{
		/// <summary>
		/// To calculate commission.
		/// </summary>
		/// <param name="message">The message containing the information about the order or own trade.</param>
		/// <returns>The commission. If the commission can not be calculated then <see langword="null" /> will be returned.</returns>
		protected override decimal? OnProcessExecution(ExecutionMessage message)
		{
			if (message.ExecutionType == ExecutionTypes.Order)
				return (decimal)Value;
			
			return null;
		}
	}

	/// <summary>
	/// Trade commission.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str506Key)]
	[DescriptionLoc(LocalizedStrings.Str661Key)]
	public class CommissionPerTradeRule : CommissionRule
	{
		/// <summary>
		/// To calculate commission.
		/// </summary>
		/// <param name="message">The message containing the information about the order or own trade.</param>
		/// <returns>The commission. If the commission can not be calculated then <see langword="null" /> will be returned.</returns>
		protected override decimal? OnProcessExecution(ExecutionMessage message)
		{
			if (message.ExecutionType == ExecutionTypes.Trade)
				return (decimal)Value;
			
			return null;
		}
	}

	/// <summary>
	/// Order volume commission.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str662Key)]
	[DescriptionLoc(LocalizedStrings.Str663Key)]
	public class CommissionPerOrderVolumeRule : CommissionRule
	{
		/// <summary>
		/// To calculate commission.
		/// </summary>
		/// <param name="message">The message containing the information about the order or own trade.</param>
		/// <returns>The commission. If the commission can not be calculated then <see langword="null" /> will be returned.</returns>
		protected override decimal? OnProcessExecution(ExecutionMessage message)
		{
			if (message.ExecutionType == ExecutionTypes.Order)
				return (decimal)(message.Volume * Value);
			
			return null;
		}
	}

	/// <summary>
	/// Trade volume commission.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str664Key)]
	[DescriptionLoc(LocalizedStrings.Str665Key)]
	public class CommissionPerTradeVolumeRule : CommissionRule
	{
		/// <summary>
		/// To calculate commission.
		/// </summary>
		/// <param name="message">The message containing the information about the order or own trade.</param>
		/// <returns>The commission. If the commission can not be calculated then <see langword="null" /> will be returned.</returns>
		protected override decimal? OnProcessExecution(ExecutionMessage message)
		{
			if (message.ExecutionType == ExecutionTypes.Trade)
				return (decimal)(message.Volume * Value);
			
			return null;
		}
	}

	/// <summary>
	/// Number of orders commission.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str666Key)]
	[DescriptionLoc(LocalizedStrings.Str667Key)]
	public class CommissionPerOrderCountRule : CommissionRule
	{
		private int _currentCount;
		private int _count;

		/// <summary>
		/// Order count.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str668Key)]
		[DescriptionLoc(LocalizedStrings.Str669Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public int Count
		{
			get { return _count; }
			set
			{
				_count = value;
				Title = value.To<string>();
			}
		}

		/// <summary>
		/// To reset the state.
		/// </summary>
		public override void Reset()
		{
			_currentCount = 0;
			base.Reset();
		}

		/// <summary>
		/// To calculate commission.
		/// </summary>
		/// <param name="message">The message containing the information about the order or own trade.</param>
		/// <returns>The commission. If the commission can not be calculated then <see langword="null" /> will be returned.</returns>
		protected override decimal? OnProcessExecution(ExecutionMessage message)
		{
			if (message.ExecutionType != ExecutionTypes.Order)
				return null;

			if (++_currentCount < Count)
				return null;

			_currentCount = 0;
			return (decimal)Value;
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("Count", Count);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Count = storage.GetValue<int>("Count");
		}
	}

	/// <summary>
	/// Number of trades commission.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str670Key)]
	[DescriptionLoc(LocalizedStrings.Str671Key)]
	public class CommissionPerTradeCountRule : CommissionRule
	{
		private int _currentCount;
		private int _count;

		/// <summary>
		/// Number of trades.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.TradesOfKey)]
		[DescriptionLoc(LocalizedStrings.Str232Key, true)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public int Count
		{
			get { return _count; }
			set
			{
				_count = value;
				Title = value.To<string>();
			}
		}

		/// <summary>
		/// To reset the state.
		/// </summary>
		public override void Reset()
		{
			_currentCount = 0;
			base.Reset();
		}

		/// <summary>
		/// To calculate commission.
		/// </summary>
		/// <param name="message">The message containing the information about the order or own trade.</param>
		/// <returns>The commission. If the commission can not be calculated then <see langword="null" /> will be returned.</returns>
		protected override decimal? OnProcessExecution(ExecutionMessage message)
		{
			if (message.ExecutionType != ExecutionTypes.Trade)
				return null;

			if (++_currentCount < Count)
				return null;

			_currentCount = 0;
			return (decimal)Value;
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("Count", Count);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Count = storage.GetValue<int>("Count");
		}
	}

	/// <summary>
	/// Trade price commission.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str672Key)]
	[DescriptionLoc(LocalizedStrings.Str673Key)]
	public class CommissionPerTradePriceRule : CommissionRule
	{
		/// <summary>
		/// To calculate commission.
		/// </summary>
		/// <param name="message">The message containing the information about the order or own trade.</param>
		/// <returns>The commission. If the commission can not be calculated then <see langword="null" /> will be returned.</returns>
		protected override decimal? OnProcessExecution(ExecutionMessage message)
		{
			if (message.ExecutionType == ExecutionTypes.Trade)
				return (decimal)(message.TradePrice * message.Volume * Value);
			
			return null;
		}
	}

	/// <summary>
	/// Security commission.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.SecurityKey)]
	[DescriptionLoc(LocalizedStrings.Str674Key)]
	public class CommissionSecurityIdRule : CommissionRule
	{
		private SecurityId _securityId;

		/// <summary>
		/// Security ID.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.SecurityIdKey)]
		[DescriptionLoc(LocalizedStrings.SecurityIdKey, true)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public SecurityId SecurityId
		{
			get { return _securityId; }
			set
			{
				_securityId = value;
				Title = value.ToString();
			}
		}

		/// <summary>
		/// To calculate commission.
		/// </summary>
		/// <param name="message">The message containing the information about the order or own trade.</param>
		/// <returns>The commission. If the commission can not be calculated then <see langword="null" /> will be returned.</returns>
		protected override decimal? OnProcessExecution(ExecutionMessage message)
		{
			if (message.ExecutionType == ExecutionTypes.Trade && message.SecurityId == SecurityId)
				return (decimal)Value;
			
			return null;
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("SecurityId", SecurityId);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			SecurityId = storage.GetValue<SecurityId>("SecurityId");
		}
	}

	/// <summary>
	/// Security type commission.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str675Key)]
	[DescriptionLoc(LocalizedStrings.Str676Key)]
	public class CommissionSecurityTypeRule : CommissionRule
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CommissionSecurityTypeRule"/>.
		/// </summary>
		public CommissionSecurityTypeRule()
		{
			SecurityType = SecurityTypes.Stock;
		}

		private SecurityTypes _securityType;

		/// <summary>
		/// Security type.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.TypeKey)]
		[DescriptionLoc(LocalizedStrings.Str360Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public SecurityTypes SecurityType
		{
			get { return _securityType; }
			set
			{
				_securityType = value;
				Title = value.ToString();
			}
		}

		/// <summary>
		/// To calculate commission.
		/// </summary>
		/// <param name="message">The message containing the information about the order or own trade.</param>
		/// <returns>The commission. If the commission can not be calculated then <see langword="null" /> will be returned.</returns>
		protected override decimal? OnProcessExecution(ExecutionMessage message)
		{
			if (message.ExecutionType == ExecutionTypes.Trade && message.SecurityId.SecurityType == SecurityType)
				return (decimal)Value;
			
			return null;
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("SecurityType", SecurityType);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			SecurityType = storage.GetValue<SecurityTypes>("SecurityType");
		}
	}

	/// <summary>
	/// Board commission.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.BoardKey)]
	[DescriptionLoc(LocalizedStrings.BoardCommissionKey)]
	public class CommissionBoardCodeRule : CommissionRule
	{
		private string _boardCode;

		/// <summary>
		/// Board code.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.BoardKey)]
		[DescriptionLoc(LocalizedStrings.BoardCodeKey)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public string BoardCode
		{
			get { return _boardCode; }
			set
			{
				_boardCode = value;
				Title = value;
			}
		}

		/// <summary>
		/// To calculate commission.
		/// </summary>
		/// <param name="message">The message containing the information about the order or own trade.</param>
		/// <returns>The commission. If the commission can not be calculated then <see langword="null" /> will be returned.</returns>
		protected override decimal? OnProcessExecution(ExecutionMessage message)
		{
			if (message.ExecutionType == ExecutionTypes.Trade && message.SecurityId.BoardCode.CompareIgnoreCase(BoardCode))
				return (decimal)Value;
			
			return null;
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("BoardCode", BoardCode);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			BoardCode = storage.GetValue<string>("BoardCode");
		}
	}

	/// <summary>
	/// Turnover commission.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.TurnoverKey)]
	[DescriptionLoc(LocalizedStrings.TurnoverCommissionKey)]
	public class CommissionTurnOverRule : CommissionRule
	{
		private decimal _currentTurnOver;
		private decimal _turnOver;

		/// <summary>
		/// Turnover.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.TurnoverKey)]
		[DescriptionLoc(LocalizedStrings.TurnoverKey, true)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public decimal TurnOver
		{
			get { return _turnOver; }
			set
			{
				_turnOver = value;
				Title = value.To<string>();
			}
		}

		/// <summary>
		/// To reset the state.
		/// </summary>
		public override void Reset()
		{
			_turnOver = 0;
			base.Reset();
		}

		/// <summary>
		/// To calculate commission.
		/// </summary>
		/// <param name="message">The message containing the information about the order or own trade.</param>
		/// <returns>The commission. If the commission can not be calculated then <see langword="null" /> will be returned.</returns>
		protected override decimal? OnProcessExecution(ExecutionMessage message)
		{
			if (message.ExecutionType != ExecutionTypes.Trade)
				return null;

			_currentTurnOver += message.GetTradePrice() * message.SafeGetVolume();

			if (_currentTurnOver < TurnOver)
				return null;

			return (decimal)Value;
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("TurnOver", TurnOver);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			TurnOver = storage.GetValue<decimal>("TurnOver");
		}
	}
}