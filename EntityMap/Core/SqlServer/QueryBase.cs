using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;

namespace EntityMap.Core.SqlServer {
	public abstract class QueryBase : IQuery {
		public string EntityName { get; set; }
		public IEnumerable<string> ColumnSet { get; set; }
		public Dictionary<string, object> Parameters { get; set; }

		public QueryBase (string entityName, IEnumerable<string> columnSet = null) {
			this.EntityName = entityName;
			this.ColumnSet = columnSet != null ? columnSet : new List<string>();
			this.Parameters = new Dictionary<string, object>();
		}

		public IEnumerable<Entity> GetResult (IEntitySession session) {
			Entity entity = session.CreateEntity(this.EntityName);

			string sql = this.GetQuery(entity);

			session.Open();
			using (IDbCommand command = session.GetCommand(sql)) {

				foreach (string key in this.Parameters.Keys) {
					SqlParameter parameter = new SqlParameter(key, this.Parameters[key]);

					if (entity.GetInternalProperties().ContainsKey(key))
						parameter.SqlDbType = (SqlDbType)entity.GetInternalProperties()[key].ProviderType;
					else
						parameter.DbType = TypeMap[this.Parameters[key].GetType()];

					command.Parameters.Add(parameter);
				}

				using (IDataReader reader = command.ExecuteReader()) {
					IEnumerable<string> columns = this.ColumnSet.Any() ? this.ColumnSet : entity.GetInternalProperties().Keys;
					List<Entity> entities = new List<Entity>();
					while (reader.Read()) {
						Entity mappedEntity = entity.Clone() as Entity;

						foreach (string column in columns) {
							int ordinal = reader.GetOrdinal(column);
							object value = reader.GetValue(ordinal);

							if (value == DBNull.Value)
								value = null;

							mappedEntity.SetValue(column, value);
						}
						entities.Add(mappedEntity);
					}
					return entities;
				}
			}
		}

		protected abstract string GetQuery (Entity entity);

		private static ReaderWriterLock cachelock = new ReaderWriterLock();

		private static Dictionary<Type, DbType> _typeMap;

		private static Dictionary<Type, DbType> TypeMap {
			get {
				if (_typeMap == null) {
					try {
						cachelock.AcquireWriterLock(-1);
						_typeMap = new Dictionary<Type, DbType>();
						_typeMap[typeof(byte)] = DbType.Byte;
						_typeMap[typeof(sbyte)] = DbType.SByte;
						_typeMap[typeof(short)] = DbType.Int16;
						_typeMap[typeof(ushort)] = DbType.UInt16;
						_typeMap[typeof(int)] = DbType.Int32;
						_typeMap[typeof(uint)] = DbType.UInt32;
						_typeMap[typeof(long)] = DbType.Int64;
						_typeMap[typeof(ulong)] = DbType.UInt64;
						_typeMap[typeof(float)] = DbType.Single;
						_typeMap[typeof(double)] = DbType.Double;
						_typeMap[typeof(decimal)] = DbType.Decimal;
						_typeMap[typeof(bool)] = DbType.Boolean;
						_typeMap[typeof(string)] = DbType.String;
						_typeMap[typeof(char)] = DbType.StringFixedLength;
						_typeMap[typeof(Guid)] = DbType.Guid;
						_typeMap[typeof(DateTime)] = DbType.DateTime;
						_typeMap[typeof(DateTimeOffset)] = DbType.DateTimeOffset;
						_typeMap[typeof(TimeSpan)] = DbType.Time;
						_typeMap[typeof(byte[])] = DbType.Binary;
						_typeMap[typeof(byte?)] = DbType.Byte;
						_typeMap[typeof(sbyte?)] = DbType.SByte;
						_typeMap[typeof(short?)] = DbType.Int16;
						_typeMap[typeof(ushort?)] = DbType.UInt16;
						_typeMap[typeof(int?)] = DbType.Int32;
						_typeMap[typeof(uint?)] = DbType.UInt32;
						_typeMap[typeof(long?)] = DbType.Int64;
						_typeMap[typeof(ulong?)] = DbType.UInt64;
						_typeMap[typeof(float?)] = DbType.Single;
						_typeMap[typeof(double?)] = DbType.Double;
						_typeMap[typeof(decimal?)] = DbType.Decimal;
						_typeMap[typeof(bool?)] = DbType.Boolean;
						_typeMap[typeof(char?)] = DbType.StringFixedLength;
						_typeMap[typeof(Guid?)] = DbType.Guid;
						_typeMap[typeof(DateTime?)] = DbType.DateTime;
						_typeMap[typeof(DateTimeOffset?)] = DbType.DateTimeOffset;
						_typeMap[typeof(TimeSpan?)] = DbType.Time;
						_typeMap[typeof(Object)] = DbType.Object;
					} finally {
						cachelock.ReleaseWriterLock();
					}
				}
				return _typeMap;
			}
		}
	}
}
