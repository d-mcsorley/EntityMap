using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EntityMap.Core {
	public class Property {
		public string Name { get; set; }
		public Type DataType { get; set; }
		public Type ProviderSpecificDataType { get; set; }
		public int ProviderType { get; set; }
		public int Ordinal { get; set; }
		public int Size { get; set; }
		public bool AllowNull { get; set; }

		public Property (string name, Type dataType, Type providerSpecificDataType, int providerType, int ordinal, int size, bool allowNull) {
			this.Name = name;
			this.DataType = dataType;
			this.ProviderSpecificDataType = providerSpecificDataType;
			this.ProviderType = providerType;
			this.Ordinal = ordinal;
			this.Size = size;
			this.AllowNull = allowNull;
		}
	}
}
