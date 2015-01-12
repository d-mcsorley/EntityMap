using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace EntityMap.Core {
	public interface IEntitySession : IDisposable {
		void Open ();
		void Close ();
		IUnitOfWork CreateUnitOfWork ();
		IUnitOfWork CreateUnitOfWork (IsolationLevel isolationLevel);
		IDbConnection Connection { get; }
		IDbTransaction Transaction { get; }
		bool UnitOfWorkActive { get; }	
		IDbCommand GetCommand (string sql, int? commandTimeout = null, CommandType commandType = CommandType.Text, params IDbDataParameter[] parameters);
		Entity CreateEntity (string entityName);
		Entity CreateEntity (string entityName, IDictionary<string, object> properties);
		void Create (Entity entity);
		Entity Retrieve (string entityName, object id);
		Entity Retrieve (string entityName, object id, params string[] columns);
		IEnumerable<Entity> RetrieveMultiple (string entityName, int pageNumber, int pageSize, IEnumerable<OrderExpression> orders);
		IEnumerable<Entity> RetrieveMultiple (string entityName, int pageNumber, int pageSize, IEnumerable<OrderExpression> orders, params string[] columns);
		void Update (Entity entity);
		void Delete (string entityName, object id);
	}
}
