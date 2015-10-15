namespace StockSharp.Algo.Candles
{
	using System;
	using System.ComponentModel;

	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// Candles series.
	/// </summary>
	public class CandleSeries : Disposable, IPersistable, INotifyPropertyChanged
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CandleSeries"/>.
		/// </summary>
		public CandleSeries()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CandleSeries"/>.
		/// </summary>
		/// <param name="candleType">The candle type.</param>
		/// <param name="security">The instrument to be used for candles formation.</param>
		/// <param name="arg">The candle formation parameter. For example, for <see cref="TimeFrameCandle"/> this value is <see cref="TimeFrameCandle.TimeFrame"/>.</param>
		public CandleSeries(Type candleType, Security security, object arg)
		{
			if (candleType == null)
				throw new ArgumentNullException("candleType");

			if (!candleType.IsSubclassOf(typeof(Candle)))
				throw new ArgumentOutOfRangeException("candleType", candleType, "Íåïðàâèëüíûé òèï ñâå÷êè.");

			if (security == null)
				throw new ArgumentNullException("security");

			if (arg == null)
				throw new ArgumentNullException("arg");

			_security = security;
			_candleType = candleType;
			_arg = arg;
			WorkingTime = security.CheckExchangeBoard().WorkingTime;
		}

		private Security _security;

		/// <summary>
		/// The instrument to be used for candles formation.
		/// </summary>
		public virtual Security Security
		{
			get { return _security; }
			set
			{
				_security = value;
				RaisePropertyChanged("Security");
			}
		}

		private Type _candleType;

		/// <summary>
		/// The candle type.
		/// </summary>
		public virtual Type CandleType
		{
			get { return _candleType; }
			set
			{
				_candleType = value;
				RaisePropertyChanged("CandleType");
			}
		}

		private object _arg;

		/// <summary>
		/// The candle formation parameter. For example, for <see cref="TimeFrameCandle"/> this value is <see cref="TimeFrameCandle.TimeFrame"/>.
		/// </summary>
		public virtual object Arg
		{
			get { return _arg; }
			set
			{
				_arg = value;
				RaisePropertyChanged("Arg");
			}
		}

		/// <summary>
		/// The time boundary, within which candles for give series shall be translated.
		/// </summary>
		public WorkingTime WorkingTime { get; set; }

		private ICandleManager _candleManager;

		/// <summary>
		/// The candle manager, which has registered given series.
		/// </summary>
		public ICandleManager CandleManager
		{
			get { return _candleManager; }
			set
			{
				if (value != _candleManager)
				{
					if (_candleManager != null)
					{
						_candleManager.Processing -= CandleManagerProcessing;
						_candleManager.Stopped -= CandleManagerStopped;
					}

					_candleManager = value;

					if (_candleManager != null)
					{
						_candleManager.Processing += CandleManagerProcessing;
						_candleManager.Stopped += CandleManagerStopped;
					}
				}
			}
		}

		/// <summary>
		/// To perform the calculation <see cref="Candle.VolumeProfileInfo"/>. By default, it is disabled.
		/// </summary>
		public bool IsCalcVolumeProfile { get; set; }

		// èñïîëüçóåòñÿ RealTimeCandleBuilderSource
		internal bool IsNew { get; set; }

		/// <summary>
		/// The candle processing event.
		/// </summary>
		public event Action<Candle> ProcessCandle;

		/// <summary>
		/// The series processing end event.
		/// </summary>
		public event Action Stopped;

		private DateTimeOffset _from = DateTimeOffset.MinValue;

		/// <summary>
		/// The initial date from which you need to get data.
		/// </summary>
		public DateTimeOffset From
		{
			get { return _from; }
			set { _from = value; }
		}

		private DateTimeOffset _to = DateTimeOffset.MaxValue;
		
		/// <summary>
		/// The final date by which you need to get data.
		/// </summary>
		public DateTimeOffset To
		{
			get { return _to; }
			set { _to = value; }
		}

		private void CandleManagerStopped(CandleSeries series)
		{
			if (series == this)
				Stopped.SafeInvoke();
		}

		private void CandleManagerProcessing(CandleSeries series, Candle candle)
		{
			if (series == this)
				ProcessCandle.SafeInvoke(candle);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			base.DisposeManaged();

			if (CandleManager != null)
				CandleManager.Stop(this);
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return CandleType.Name + "_" + Security + "_" + TraderHelper.CandleArgToFolderName(Arg);
		}

		#region IPersistable

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Load(SettingsStorage storage)
		{
			var connector = ConfigManager.TryGetService<IConnector>();
			if (connector != null)
			{
				var securityId = storage.GetValue<string>("SecurityId");

				if (!securityId.IsEmpty())
					Security = connector.LookupById(securityId);
			}

			CandleType = storage.GetValue<Type>("CandleType");
			Arg = storage.GetValue<object>("Arg");

			From = storage.GetValue<DateTimeOffset>("From");
			To = storage.GetValue<DateTimeOffset>("To");
			WorkingTime = storage.GetValue<WorkingTime>("WorkingTime");

			IsCalcVolumeProfile = storage.GetValue<bool>("IsCalcVolumeProfile");
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Save(SettingsStorage storage)
		{
			if (Security != null)
				storage.SetValue("SecurityId", Security.Id);

			storage.SetValue("CandleType", CandleType.GetTypeName(false));
			storage.SetValue("Arg", Arg);

			storage.SetValue("From", From);
			storage.SetValue("To", To);
			storage.SetValue("WorkingTime", WorkingTime);

			storage.SetValue("IsCalcVolumeProfile", IsCalcVolumeProfile);
		}

		#endregion

		#region INotifyPropertyChanged

		/// <summary>
		/// The series parameters change event.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// To call series parameters change event.
		/// </summary>
		protected void RaisePropertyChanged(string propertyName)
		{
			PropertyChanged.SafeInvoke(this, propertyName);
		}

		#endregion
	}
}