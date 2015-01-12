using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace EntityMap.Core.SqlServer {
	public class SqlEntitySession : IEntitySession {
		private IDbConnection _connection;
		private IDbTransaction _transaction = null;
		private bool _isDisposed = false;
		private UnitOfWork _unitOfWork;
		private static ConcurrentDictionary<string, Entity> _entityTemplates = new ConcurrentDictionary<string, Entity>();

        public SqlEntitySession(string connectionString) {
			_connection = new SqlConnection(connectionString);
		}

		public SqlEntitySession (IDbConnection connection) {
			_connection = connection;
		}

		public IDbConnection Connection {
			get { return _connection; }
		}

		public IDbTransaction Transaction {
			get { return _transaction; }
		}

		public void Open () {
			if (_connection.State == ConnectionState.Closed)
				_connection.Open();
		}

		public void Close () {
			if (_connection != null && _connection.State != ConnectionState.Closed)
				_connection.Close();
		}

		public bool UnitOfWorkActive {
			get { return _unitOfWork == null ? false : true; }
		}

		public IUnitOfWork CreateUnitOfWork () {
			this.Open();
			_transaction = _connection.BeginTransaction();
			_unitOfWork = new UnitOfWork(_transaction, RemoveUnitOfWork, RemoveUnitOfWork);
			return _unitOfWork;
		}

		public IUnitOfWork CreateUnitOfWork (IsolationLevel isolationLevel) {
			this.Open();
			_transaction = _connection.BeginTransaction(isolationLevel);
			_unitOfWork = new UnitOfWork(_transaction, RemoveUnitOfWork, RemoveUnitOfWork);
			return _unitOfWork;
		}

		private void RemoveUnitOfWork (UnitOfWork unitOfWork) {
			_transaction = null;
			unitOfWork = null;
		}

		public IDbCommand GetCommand (string sql, int? commandTimeout = null, CommandType commandType = CommandType.Text, params IDbDataParameter[] parameters) {
			IDbCommand command = this.Connection.CreateCommand();
			command.CommandText = sql;
			command.CommandType = commandType;
			command.Transaction = this.Transaction;

			if (commandTimeout.HasValue)
				command.CommandTimeout = commandTimeout.Value;

			if (parameters != null) {
				foreach (IDbDataParameter parameter in parameters) {
					if (parameter.Value == null)
						parameter.Value = DBNull.Value;

					command.Parameters.Add(parameter);
				}
			}
			return command;
		}

		/// <summary>
		/// Dispose of the DbSession.
		/// </summary>
		public void Dispose () {
			if (!_isDisposed) {
				this.Close();
				_connection.Dispose();

				if (_transaction != null)
					_transaction.Dispose();

				_isDisposed = true;
			}
		}

        public Entity CreateEntity(string entityName) {
            if (entityName == null) throw new ArgumentNullException("entityName");

            if (!_entityTemplates.ContainsKey(entityName)) {
                string sql = String.Format(@"SELECT TOP 1 * FROM {0}", entityName);
                this.Open();

                using (IDbCommand command = this.GetCommand(sql))
                using (IDataReader reader = command.ExecuteReader()) {

                    DataTable schemaTable = reader.GetSchemaTable();

                    IList<Property> properties = new List<Property>();

                    foreach (DataRow myField in schemaTable.Rows) {
                        string name = myField["ColumnName"].ToString();
                        Type dataType = myField["DataType"] as Type;
                        Type providerSpecificDataType = myField["ProviderSpecificDataType"] as Type;
                        int providerType = (int)myField["ProviderType"];
                        int ordinal = (int)myField["ColumnOrdinal"];
                        int size = (int)myField["ColumnSize"];
                        bool allowNull = (bool)myField["AllowDBNull"];

                        properties.Add(new Property(name, dataType, providerSpecificDataType, providerType, ordinal, size, allowNull));
                    }

                    Entity entity = new Entity(entityName, properties);
                    _entityTemplates.TryAdd(entityName, entity);
                }
            }
            return _entityTemplates[entityName].Clone() as Entity;
        }

        public Entity CreateEntity(string entityName, IDictionary<string, object> properties) {
            if (entityName == null) throw new ArgumentNullException("entityName");
            if (properties == null) throw new ArgumentNullException("properties");

            Entity entity = this.CreateEntity(entityName);

            foreach (string key in properties.Keys) {
                entity.SetValue(key, properties[key]);
            }
            return entity;
        }

        public void Create(Entity entity) {
            if (entity == null) throw new ArgumentNullException("entity");
            if (!entity.Contains("Id")) throw new ValuePropertyException("Property: 'Id' is missing from the entity. An entity must have an Id assigned.");

            List<IDbDataParameter> dbParameters = new List<IDbDataParameter>();

            StringBuilder columnsBuilder = new StringBuilder();
            StringBuilder valuesBuilder = new StringBuilder();

            foreach (string key in entity.Properties.Keys) {
                columnsBuilder.AppendFormat(@"{0}, ", key);
                valuesBuilder.AppendFormat(@"@{0}, ", key);

                SqlParameter sqlParameter = new SqlParameter(key, entity.Properties[key].Value);
                sqlParameter.SqlDbType = (SqlDbType)entity.GetInternalProperties()[key].ProviderType;

                if (sqlParameter.Value == null)
                    sqlParameter.Value = DBNull.Value;


                dbParameters.Add(sqlParameter);
            }

            columnsBuilder.Remove(columnsBuilder.Length - 2, 2);
            valuesBuilder.Remove(valuesBuilder.Length - 2, 2);

            string sql = String.Format(@"INSERT INTO {0} ({1}) VALUES ({2})", entity.Name, columnsBuilder.ToString(), valuesBuilder.ToString());

            this.Open();
            using (IDbCommand command = this.GetCommand(sql, null, CommandType.Text, dbParameters.ToArray())) {
                command.ExecuteNonQuery();
            }
        }

        public Entity Retrieve(string entityName, object id) {
            if (entityName == null) throw new ArgumentNullException("entityName");
            if (id == null) throw new ArgumentNullException("id");
            
            return new QuerySingle(entityName, id).GetResult(this).FirstOrDefault();
        }

        public Entity Retrieve(string entityName, object id, params string[] columns) {
            if (entityName == null) throw new ArgumentNullException("entityName");
            if (id == null) throw new ArgumentNullException("id");
            if (columns == null) throw new ArgumentNullException("columns");
            
            return new QuerySingle(entityName, id, columns).GetResult(this).FirstOrDefault();
        }

        public IEnumerable<Entity> RetrieveMultiple(string entityName, int pageNumber, int pageSize, IEnumerable<OrderExpression> orders) {
            if (entityName == null) throw new ArgumentNullException("entityName");
            if (pageNumber < 0) throw new ArgumentOutOfRangeException("pageNumber", "Argument: 'pageNumber' cannot be less than zero (0).");
            if (pageSize < 0) throw new ArgumentOutOfRangeException("pageSize", "Argument: 'pageSize' cannot be less than zero (0).");
            if (orders == null) throw new ArgumentNullException("orders");
            
            return new QueryMultiple(entityName, pageNumber, pageSize, orders).GetResult(this);
        }

        public IEnumerable<Entity> RetrieveMultiple(string entityName, int pageNumber, int pageSize, IEnumerable<OrderExpression> orders, params string[] columns) {
            if (entityName == null) throw new ArgumentNullException("entityName");
            if (pageNumber < 0) throw new ArgumentOutOfRangeException("pageNumber", "Argument: 'pageNumber' cannot be less than zero (0).");
            if (pageSize < 0) throw new ArgumentOutOfRangeException("pageSize", "Argument: 'pageSize' cannot be less than zero (0).");
            if (orders == null) throw new ArgumentNullException("orders");
            if (columns == null) throw new ArgumentNullException("columns");
            
            return new QueryMultiple(entityName, pageNumber, pageSize, orders, columns).GetResult(this);
        }

        public void Update(Entity entity) {
            if (entity == null) throw new ArgumentNullException("entity");
            if (!entity.Properties.ContainsKey("Id")) throw new ValuePropertyException("Property: 'Id' is missing from the entity. An update entity must have an Id assigned.");

            List<IDbDataParameter> dbParameters = new List<IDbDataParameter>();

            StringBuilder sqlStringBuilder = new StringBuilder();
            sqlStringBuilder.AppendFormat(@"UPDATE [{0}] SET ", entity.Name);

            foreach (string key in entity.Properties.Keys) {
                SqlParameter sqlParameter = new SqlParameter(key, entity.Properties[key].Value);
                sqlParameter.SqlDbType = (SqlDbType)entity.GetInternalProperties()[key].ProviderType;

                if (sqlParameter.Value == null)
                    sqlParameter.Value = DBNull.Value;

                dbParameters.Add(sqlParameter);

                if (key == "Id") continue;

                sqlStringBuilder.AppendFormat(@"[{0}] = @{0}, ", key);
            }

            sqlStringBuilder.Remove(sqlStringBuilder.Length - 2, 2);
            sqlStringBuilder.Append(@" WHERE [Id] = @Id");

            this.Open();
            using (IDbCommand command = this.GetCommand(sqlStringBuilder.ToString(), null, CommandType.Text, dbParameters.ToArray())) {
                command.ExecuteNonQuery();
            }
        }

        public void Delete(string entityName, object id) {
            if (entityName == null) throw new ArgumentNullException("entityName");
            if (id == null) throw new ArgumentNullException("id");

            string sql = String.Format(@"DELETE FROM [{0}] WHERE Id = @id", entityName);

            this.Open();
            using (IDbCommand command = this.GetCommand(sql, null, CommandType.Text, new SqlParameter("id", id))) {
                command.ExecuteNonQuery();
            }
        }
    }
}
