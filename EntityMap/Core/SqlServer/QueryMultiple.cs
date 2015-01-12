using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EntityMap.Core.SqlServer {
	public class QueryMultiple : QueryBase {
		private IEnumerable<OrderExpression> _orders;
		private int _rowNumber;

		public QueryMultiple (string entityName, int pageNumber, int pageSize, IEnumerable<OrderExpression> orders)
			: base(entityName) {
			if (pageNumber <= 0) throw new ArgumentOutOfRangeException("pageNumber", "The page number cannot be zero or less than zero.");

			this._rowNumber = pageNumber == 1 ? 0 : pageSize * (pageNumber - 1);

			this.Parameters.Add("rowNumber", this._rowNumber);
			this.Parameters.Add("pageSize", pageSize);
			this._orders = orders;
		}

		public QueryMultiple (string entityName, int pageNumber, int pageSize, IEnumerable<OrderExpression> orders, params string[] columns)
			: base(entityName, columns) {
			if (pageNumber <= 0) throw new ArgumentOutOfRangeException("pageNumber", "The page number cannot be zero or less than zero.");

			this._rowNumber = pageNumber == 1 ? 0 : pageSize * (pageNumber - 1);

			this.Parameters.Add("rowNumber", this._rowNumber);
			this.Parameters.Add("pageSize", pageSize);
			this._orders = orders;
		}

		protected override string GetQuery (Entity entity) {
			StringBuilder sqlStringBuilder = new StringBuilder();

			// Begin SELECT statement.
			sqlStringBuilder.Append(@"SELECT TOP (@pageSize) [RowNumber], ");

			// Get the index for the start of the columns.
			int startColumnsPosition = sqlStringBuilder.Length;

			// Write the columns to the sql statement.
			if (this.ColumnSet.Any()) {
				foreach (string column in this.ColumnSet) {
					sqlStringBuilder.AppendFormat(@"[{0}].{1}, ", entity.Name, column);
				}
			} else {
				foreach (string key in entity.GetInternalProperties().Keys) {
					sqlStringBuilder.AppendFormat(@"[{0}].{1}, ", entity.Name, key);
				}
			}

			// Remove the trailing comma.
			sqlStringBuilder.Remove(sqlStringBuilder.Length - 2, 2);

			// This isn't necessarily the easiest or most readable way of doing this however it is the most efficient. Using the index for the start of the
			// columns we copy the chars into an array so we can write them to the StringBuilder later without having to perform another loop.
			char[] columnsCharArray = new char[sqlStringBuilder.Length - startColumnsPosition];
			sqlStringBuilder.CopyTo(startColumnsPosition, columnsCharArray, 0, sqlStringBuilder.Length - startColumnsPosition);

			sqlStringBuilder.Append(@" FROM (SELECT Row_Number() OVER (ORDER BY ");

			// Get the index for the start of the ORDER BY items.
			int startOrdersPosition = sqlStringBuilder.Length;

			// Write the ORDER BY items to the sql statement.
			foreach (OrderExpression orderExpression in this._orders) {
				sqlStringBuilder.AppendFormat(@"[{0}].{1} {2}, ", entity.Name, orderExpression.ColumnName, orderExpression.OrderType.ToString());
			}

			// Remove the trailing comma.
			sqlStringBuilder.Remove(sqlStringBuilder.Length - 2, 2);

			// Same as above copy the ORDER BY segment of the statement to an array.
			char[] ordersCharArray = new char[sqlStringBuilder.Length - startOrdersPosition];
			sqlStringBuilder.CopyTo(startOrdersPosition, ordersCharArray, 0, sqlStringBuilder.Length - startOrdersPosition);

			sqlStringBuilder.Append(" ) AS [RowNumber], ");
			sqlStringBuilder.Append(columnsCharArray);
			sqlStringBuilder.AppendFormat(@" FROM [{0}] GROUP BY ", entity.Name);
			sqlStringBuilder.Append(columnsCharArray);
			sqlStringBuilder.AppendFormat(@") AS [{0}]", entity.Name);
			sqlStringBuilder.AppendFormat(@" WHERE [{0}].[RowNumber] > @rowNumber ORDER BY ", entity.Name);
			sqlStringBuilder.Append(ordersCharArray);

			return sqlStringBuilder.ToString();
		}

	}
}
