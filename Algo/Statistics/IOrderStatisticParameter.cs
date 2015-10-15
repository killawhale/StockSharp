namespace StockSharp.Algo.Statistics
{
	using System;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;
	using StockSharp.Localization;

	/// <summary>
	/// The interface, describing statistic parameter, calculated based on orders.
	/// </summary>
	public interface IOrderStatisticParameter
	{
		/// <summary>
		/// To add to the parameter an information on new order.
		/// </summary>
		/// <param name="order">New order.</param>
		void New(Order order);

		/// <summary>
		/// To add to the parameter an information on changed order.
		/// </summary>
		/// <param name="order">The changed order.</param>
		void Changed(Order order);

		/// <summary>
		/// To add to the parameter an information on error of order registration.
		/// </summary>
		/// <param name="fail">Error registering order.</param>
		void RegisterFailed(OrderFail fail);
	
		/// <summary>
		/// To add to the parameter an information on error of order cancelling.
		/// </summary>
		/// <param name="fail">Error cancelling order.</param>
		void CancelFailed(OrderFail fail);
	}

	/// <summary>
	/// The base statistic parameter, calculated based on orders.
	/// </summary>
	/// <typeparam name="TValue">The type of the parameter value.</typeparam>
	public abstract class BaseOrderStatisticParameter<TValue> : BaseStatisticParameter<TValue>, IOrderStatisticParameter
		where TValue : IComparable<TValue>
	{
		/// <summary>
		/// Initialize <see cref="BaseOrderStatisticParameter{T}"/>.
		/// </summary>
		protected BaseOrderStatisticParameter()
		{
		}

		/// <summary>
		/// To add to the parameter an information on new order.
		/// </summary>
		/// <param name="order">New order.</param>
		public virtual void New(Order order)
		{
		}

		/// <summary>
		/// To add to the parameter an information on changed order.
		/// </summary>
		/// <param name="order">The changed order.</param>
		public virtual void Changed(Order order)
		{
		}

		/// <summary>
		/// To add to the parameter an information on error of order registration.
		/// </summary>
		/// <param name="fail">Error registering order.</param>
		public virtual void RegisterFailed(OrderFail fail)
		{
		}

		/// <summary>
		/// To add to the parameter an information on error of order cancelling.
		/// </summary>
		/// <param name="fail">Error cancelling order.</param>
		public virtual void CancelFailed(OrderFail fail)
		{
		}
	}

	/// <summary>
	/// The maximal value of the order registration delay.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str947Key)]
	[DescriptionLoc(LocalizedStrings.Str948Key)]
	[CategoryLoc(LocalizedStrings.OrdersKey)]
	public class MaxLatencyRegistrationParameter : BaseOrderStatisticParameter<TimeSpan>
	{
		/// <summary>
		/// To add to the parameter an information on new order.
		/// </summary>
		/// <param name="order">New order.</param>
		public override void New(Order order)
		{
			if (order.LatencyRegistration != null)
				Value = Value.Max(order.LatencyRegistration.Value);
		}
	}

	/// <summary>
	/// The maximal value of the order cancelling delay.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str950Key)]
	[DescriptionLoc(LocalizedStrings.Str951Key)]
	[CategoryLoc(LocalizedStrings.OrdersKey)]
	public class MaxLatencyCancellationParameter : BaseOrderStatisticParameter<TimeSpan>
	{
		/// <summary>
		/// To add to the parameter an information on changed order.
		/// </summary>
		/// <param name="order">The changed order.</param>
		public override void Changed(Order order)
		{
			if (order.LatencyCancellation != null)
				Value = Value.Max(order.LatencyCancellation.Value);
		}
	}

	/// <summary>
	/// The minimal value of order registration delay.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str952Key)]
	[DescriptionLoc(LocalizedStrings.Str953Key)]
	[CategoryLoc(LocalizedStrings.OrdersKey)]
	public class MinLatencyRegistrationParameter : BaseOrderStatisticParameter<TimeSpan>
	{
		private bool _initialized;

		/// <summary>
		/// To add to the parameter an information on new order.
		/// </summary>
		/// <param name="order">New order.</param>
		public override void New(Order order)
		{
			if (order.LatencyRegistration == null)
				return;

			if (!_initialized)
			{
				Value = order.LatencyRegistration.Value;
				_initialized = true;
			}
			else
				Value = Value.Min(order.LatencyRegistration.Value);
		}

		/// <summary>
		/// To load the state of statistic parameter.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Load(SettingsStorage storage)
		{
			_initialized = storage.GetValue<bool>("Initialized");
			base.Load(storage);
		}

		/// <summary>
		/// To save the state of statistic parameter.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Save(SettingsStorage storage)
		{
			storage.SetValue("Initialized", _initialized);
			base.Save(storage);
		}
	}

	/// <summary>
	/// The minimal value of order cancelling delay.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str954Key)]
	[DescriptionLoc(LocalizedStrings.Str955Key)]
	[CategoryLoc(LocalizedStrings.OrdersKey)]
	public class MinLatencyCancellationParameter : BaseOrderStatisticParameter<TimeSpan>
	{
		private bool _initialized;

		/// <summary>
		/// To add to the parameter an information on changed order.
		/// </summary>
		/// <param name="order">The changed order.</param>
		public override void Changed(Order order)
		{
			if (order.LatencyCancellation == null)
				return;

			if (!_initialized)
			{
				Value = order.LatencyCancellation.Value;
				_initialized = true;
			}
			else
				Value = Value.Min(order.LatencyCancellation.Value);
		}

		/// <summary>
		/// To load the state of statistic parameter.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Load(SettingsStorage storage)
		{
			_initialized = storage.GetValue<bool>("Initialized");
			base.Load(storage);
		}

		/// <summary>
		/// To save the state of statistic parameter.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Save(SettingsStorage storage)
		{
			storage.SetValue("Initialized", _initialized);
			base.Save(storage);
		}
	}

	/// <summary>
	/// Total number of orders.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str956Key)]
	[DescriptionLoc(LocalizedStrings.Str957Key)]
	[CategoryLoc(LocalizedStrings.OrdersKey)]
	public class OrderCountParameter : BaseOrderStatisticParameter<int>
	{
		/// <summary>
		/// To add to the parameter an information on new order.
		/// </summary>
		/// <param name="order">New order.</param>
		public override void New(Order order)
		{
			Value++;
		}
	}
}