namespace StockSharp.Algo.Storages.Csv
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	/// <summary>
	/// List of trade objects, received from the CSV storage.
	/// </summary>
	/// <typeparam name="T">Entity type.</typeparam>
	public abstract class CsvEntityList<T> : SynchronizedList<T>, IStorageEntityList<T>
		where T : class
	{
		private readonly string _fileName;

		private readonly Dictionary<object, T> _items = new Dictionary<object, T>();

		/// <summary>
		/// The CSV storage of trading objects.
		/// </summary>
		protected CsvEntityRegistry Registry { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="CsvEntityList{T}"/>.
		/// </summary>
		/// <param name="registry">The CSV storage of trading objects.</param>
		/// <param name="fileName">CSV file name.</param>
		protected CsvEntityList(CsvEntityRegistry registry, string fileName)
		{
			if (registry == null)
				throw new ArgumentNullException(nameof(registry));

			if (fileName == null)
				throw new ArgumentNullException(nameof(fileName));

			Registry = registry;

			_fileName = Path.Combine(Registry.Path, fileName);
		}

		#region IStorageEntityList<T>

		private DelayAction.Group _delayActionGroup;
		private DelayAction _delayAction;

		/// <summary>
		/// The time delayed action.
		/// </summary>
		public DelayAction DelayAction
		{
			get { return _delayAction; }
			set
			{
				if (_delayAction == value)
					return;

				if (_delayAction != null)
				{
					_delayAction.DeleteGroup(_delayActionGroup);
					_delayActionGroup = null;
				}

				_delayAction = value;

				if (_delayAction != null)
				{
					_delayActionGroup = _delayAction.CreateGroup(() => new CsvFileWriter(new TransactionFileStream(_fileName, FileMode.Append), Registry.Encoding));
				}
			}
		}

		T IStorageEntityList<T>.ReadById(object id)
		{
			lock (SyncRoot)
				return _items.TryGetValue(NormalizedKey(id));
		}

		IEnumerable<T> IStorageEntityList<T>.ReadLasts(int count)
		{
			lock (SyncRoot)
				return _items.Values.Skip(Count - count).Take(count).ToArray();
		}

		private object GetNormalizedKey(T entity)
		{
			return NormalizedKey(GetKey(entity));
		}

		private static object NormalizedKey(object key)
		{
			var str = key as string;

			if (str != null)
				return str.ToLowerInvariant();

			return key;
		}

		/// <summary>
		/// Save object into storage.
		/// </summary>
		/// <param name="entity">Trade object.</param>
		public virtual void Save(T entity)
		{
			lock (SyncRoot)
			{
				var item = _items.TryGetValue(GetNormalizedKey(entity));

				if (item == null)
				{
					Add(entity);
					return;
				}
				else if (IsChanged(entity))
					UpdateCache(entity);
				else
					return;
			}

			Write();
		}

		#endregion

		/// <summary>
		/// Is <paramref name="entity"/> changed.
		/// </summary>
		/// <param name="entity">Trade object.</param>
		/// <returns>Is changed.</returns>
		protected virtual bool IsChanged(T entity)
		{
			return true;
		}

		/// <summary>
		/// Get key from trade object.
		/// </summary>
		/// <param name="item">Trade object.</param>
		/// <returns>The key.</returns>
		protected abstract object GetKey(T item);

		/// <summary>
		/// Write data into CSV.
		/// </summary>
		/// <param name="writer">CSV writer.</param>
		/// <param name="data">Trade object.</param>
		protected abstract void Write(CsvFileWriter writer, T data);

		/// <summary>
		/// Read data from CSV.
		/// </summary>
		/// <param name="reader">CSV reader.</param>
		/// <returns>Trade object.</returns>
		protected abstract T Read(FastCsvReader reader);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public override bool Contains(T item)
		{
			lock (SyncRoot)
				return _items.ContainsKey(GetNormalizedKey(item));
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="item">Trade object.</param>
		protected override void OnAdded(T item)
		{
			base.OnAdded(item);

			lock (SyncRoot)
			{
				if (!_items.TryAdd(GetNormalizedKey(item), item))
					return;

				AddCache(item);
			}
		
			Write(item);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="item">Trade object.</param>
		protected override void OnRemoved(T item)
		{
			base.OnRemoved(item);

			lock (SyncRoot)
			{
				_items.Remove(GetNormalizedKey(item));
				RemoveCache(item);
			}
			
			Write();
		}

		/// <summary>
		/// 
		/// </summary>
		protected override void OnCleared()
		{
			base.OnCleared();

			lock (SyncRoot)
			{
				_items.Clear();
				ClearCache();
			}

			Write();
		}

		private void Write()
		{
			_delayActionGroup.Add(() =>
			{
				using (var writer = new CsvFileWriter(new TransactionFileStream(_fileName, FileMode.Create), Registry.Encoding))
				{
					T[] values;

					lock (SyncRoot)
						values = _items.Values.ToArray();

					foreach (var item in values)
						Write(writer, item);
				}
			}, canBatch: false);
		}

		private void Write(T entity)
		{
			_delayActionGroup.Add(s =>
			{
				Write((CsvFileWriter)s, entity);
			});
		}

		internal void ReadItems(List<Exception> errors)
		{
			if (errors == null)
				throw new ArgumentNullException(nameof(errors));

			if (!File.Exists(_fileName))
				return;

			CultureInfo.InvariantCulture.DoInCulture(() =>
			{
				using (var stream = new FileStream(_fileName, FileMode.OpenOrCreate))
				{
					var reader = new FastCsvReader(stream, Registry.Encoding);

					while (reader.NextLine())
					{
						try
						{
							var item = Read(reader);
							var key = GetNormalizedKey(item);

							lock (SyncRoot)
							{
								InnerCollection.Add(item);
								AddCache(item);
								_items.Add(key, item);
							}
						}
						catch (Exception ex)
						{
							if (errors.Count < 10)
								errors.Add(ex);
							else
								break;
						}
					}
				}
			});

			InnerCollection.ForEach(OnAdded);
		}

		/// <summary>
		/// Clear cache.
		/// </summary>
		protected virtual void ClearCache()
		{
		}

		/// <summary>
		/// Add item to cache.
		/// </summary>
		/// <param name="item">New item.</param>
		protected virtual void AddCache(T item)
		{
		}

		/// <summary>
		/// Update item in cache.
		/// </summary>
		/// <param name="item">Item.</param>
		protected virtual void UpdateCache(T item)
		{
		}

		/// <summary>
		/// Remove item from cache.
		/// </summary>
		/// <param name="item">Item.</param>
		protected virtual void RemoveCache(T item)
		{
		}
	}
}