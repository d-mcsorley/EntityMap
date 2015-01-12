using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EntityMap.Core {
	public class ValuePropertyException : Exception {
		public ValuePropertyException () { }

		public ValuePropertyException (string message) : base(message) { }

		public ValuePropertyException (string message, Exception innerException) : base(message, innerException) { }
	}
}
