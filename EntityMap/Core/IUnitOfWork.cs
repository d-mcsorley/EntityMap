using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EntityMap.Core {
	public interface IUnitOfWork : IDisposable {
		void SaveChanges ();
	}
}
