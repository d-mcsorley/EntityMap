using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EntityMap.Core {
	public class OrderExpression {
		public string ColumnName { get; set; }
		public OrderType OrderType { get; set; }

		public OrderExpression (string columnName, OrderType orderType) {
			this.ColumnName = columnName;
			this.OrderType = orderType;
		}
	}
}
