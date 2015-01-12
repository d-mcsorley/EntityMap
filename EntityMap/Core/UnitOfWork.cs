using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace EntityMap.Core {
	public class UnitOfWork : IUnitOfWork {
		private IDbTransaction _transaction;
		private readonly Action<UnitOfWork> _rolledBack;
		private readonly Action<UnitOfWork> _committed;

		public UnitOfWork (IDbTransaction transaction, Action<UnitOfWork> rolledBack, Action<UnitOfWork> committed) {
			_transaction = transaction;
			_rolledBack = rolledBack;
			_committed = committed;
		}

		public IDbTransaction Transaction {
			get {
				return _transaction;
			}
		}

		public void SaveChanges () {
			if (_transaction == null)
				throw new InvalidOperationException("No Transactions present for current Unit of Work");

			_transaction.Commit();
			_committed(this);
			_transaction.Dispose();
			_transaction = null;
		}

		public void Dispose () {
			if (_transaction == null)
				return;

			_transaction.Rollback();
			_rolledBack(this);
			_transaction.Dispose();
			_transaction = null;
		}
	}
}
