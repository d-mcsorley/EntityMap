using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EntityMap.Core {
	public class ValueProperty : Property {
		public object Value { get; set; }

		public ValueProperty (string name, object value, Type dataType, Type providerSpecificDataType, int providerType, int ordinal, int size, bool allowNull)
			: base(name, dataType, providerSpecificDataType, providerType, ordinal, size, allowNull) {

			if (value == null && allowNull == false)
				throw new ValuePropertyException(String.Format(@"Property: '{0}' does not allow null values.", name));

			if (value != null && value.GetType() != dataType)
				throw new ValuePropertyException(String.Format(@"Property: '{0}' type ({1}) does not match expected type ({2}).", name, value.GetType().ToString(), dataType.ToString()));

			this.Value = value;
		}
	}
}
