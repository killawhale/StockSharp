﻿namespace StockSharp.Messages
{
	using System;
	using System.Globalization;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Localization;
	using StockSharp.Logging;

	using Pair = System.Collections.Generic.KeyValuePair<System.DateTime, Messages.Message>;

	/// <summary>
	/// Транспортный канал сообщений, основанный на очереди и работающий в пределах одного процесса.
	/// </summary>
	public class InMemoryMessageChannel : IMessageChannel
	{
		private class BlockingPriorityQueue : BaseBlockingQueue<Pair, OrderedPriorityQueue<DateTime, Message>>
		{
			public BlockingPriorityQueue()
				: base(new OrderedPriorityQueue<DateTime, Message>())
			{
			}

			protected override void OnEnqueue(Pair item, bool force)
			{
				InnerCollection.Enqueue(item.Key, item.Value);
			}

			protected override Pair OnDequeue()
			{
				return InnerCollection.Dequeue();
			}

			protected override Pair OnPeek()
			{
				return InnerCollection.Peek();
			}

			public void Clear(ClearMessageQueueMessage message)
			{
				lock (SyncRoot)
				{
					switch (message.ClearMessageType)
					{
						case MessageTypes.Execution:
							InnerCollection
								.RemoveWhere(m =>
								{
									if (m.Value.Type != MessageTypes.Execution)
										return false;

									var execMsg = (ExecutionMessage)m.Value;

									return (message.SecurityId == null || execMsg.SecurityId == message.SecurityId) && (message.Arg == null || message.Arg.Compare(execMsg.ExecutionType) == 0);
								});

							break;

						case MessageTypes.QuoteChange:
							InnerCollection.RemoveWhere(m => m.Value.Type == MessageTypes.QuoteChange && (message.SecurityId == null || ((QuoteChangeMessage)m.Value).SecurityId == message.SecurityId));
							break;

						case MessageTypes.Level1Change:
							InnerCollection.RemoveWhere(m => m.Value.Type == MessageTypes.Level1Change && (message.SecurityId == null || ((Level1ChangeMessage)m.Value).SecurityId == message.SecurityId));
							break;

						case null:
							InnerCollection.Clear();
							break;
					}
				}
			}
		}

		private static readonly MemoryStatisticsValue<Message> _msgStat = new MemoryStatisticsValue<Message>(LocalizedStrings.Messages);

		static InMemoryMessageChannel()
		{
			MemoryStatistics.Instance.Values.Add(_msgStat);
		}

		private readonly Action<Exception> _errorHandler;
		private readonly BlockingPriorityQueue _messageQueue = new BlockingPriorityQueue();

		/// <summary>
		/// Создать <see cref="InMemoryMessageChannel"/>.
		/// </summary>
		/// <param name="name">Название канала.</param>
		/// <param name="errorHandler">Обработчик ошибок.</param>
		public InMemoryMessageChannel(string name, Action<Exception> errorHandler)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException("name");

			if (errorHandler == null)
				throw new ArgumentNullException("errorHandler");

			Name = name;

			_errorHandler = errorHandler;
			_messageQueue.Close();
		}

		/// <summary>
		/// Название обработчика.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Количество сообщений в очереди.
		/// </summary>
		public int MessageCount
		{
			get { return _messageQueue.Count; }
		}

		/// <summary>
		/// Максимальный размер очереди сообщений. 
		/// </summary>
		/// <remarks>
		/// Значение по умолчанию равно -1, что соответствует размеру без ограничений.
		/// </remarks>
		public int MaxMessageCount
		{
			get { return _messageQueue.MaxSize; }
			set { _messageQueue.MaxSize = value; }
		}

		/// <summary>
		/// Событие закрытия канала.
		/// </summary>
		public event Action Closed;

		/// <summary>
		/// Открыт ли канал.
		/// </summary>
		public bool IsOpened
		{
			get { return !_messageQueue.IsClosed; }
		}

		/// <summary>
		/// Открыть канал.
		/// </summary>
		public void Open()
		{
			_messageQueue.Open();

			ThreadingHelper
				.Thread(() => CultureInfo.InvariantCulture.DoInCulture(() =>
				{
					while (!_messageQueue.IsClosed)
					{
						try
						{
							Message message;

							if (!TryDequeue(out message))
								break;

							NewOutMessage.SafeInvoke(message);
						}
						catch (Exception ex)
						{
							_errorHandler(ex);
						}
					}

					Closed.SafeInvoke();
				}))
				.Name("{0} channel thread.".Put(Name))
				//.Culture(CultureInfo.InvariantCulture)
				.Launch();
		}

		private bool TryDequeue(out Message message)
		{
			Pair pair;

			if (!_messageQueue.TryDequeue(out pair))
			{
				message = null;
				return false;
			}

			_msgStat.Remove(pair.Value);

			message = pair.Value;
			return true;
		}

		/// <summary>
		/// Закрыть канал.
		/// </summary>
		public void Close()
		{
			_messageQueue.Close();
		}

		/// <summary>
		/// Отправить сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		public void SendInMessage(Message message)
		{
			if (!IsOpened)
				throw new InvalidOperationException();

			var clearMsg = message as ClearMessageQueueMessage;

			if (clearMsg != null)
			{
				_messageQueue.Clear(clearMsg);
			}
			else
			{
				_msgStat.Add(message);
				_messageQueue.Enqueue(new Pair(message.LocalTime, message));	
			}
		}

		/// <summary>
		/// Событие появления нового сообщения.
		/// </summary>
		public event Action<Message> NewOutMessage;

		void IDisposable.Dispose()
		{
			Close();
		}
	}
}