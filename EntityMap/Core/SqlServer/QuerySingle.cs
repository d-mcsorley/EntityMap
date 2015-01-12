using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EntityMap.Core.SqlServer {
	public class QuerySingle : QueryBase {

		public QuerySingle (string entityName, object id)
			: base(entityName) {
			this.Parameters.Add("id", id);
		}

		public QuerySingle (string entityName, object id, params string[] columns)
			: base(entityName, columns) {
			this.Parameters.Add("id", id);
		}

		protected override string GetQuery (Entity entity) {
			StringBuilder sqlStringBuilder = new StringBuilder();
			sqlStringBuilder.Append("SELECT ");

			if (this.ColumnSet.Any()) {
				foreach (string column in this.ColumnSet) {
					sqlStringBuilder.AppendFormat(@"[{0}].{1}, ", entity.Name, column);
				}
			} else {
				foreach (string key in entity.GetInternalProperties().Keys) {
					sqlStringBuilder.AppendFormat(@"[{0}].{1}, ", entity.Name, key);
				}
			}

			sqlStringBuilder.Remove(sqlStringBuilder.Length - 2, 2);
			sqlStringBuilder.AppendFormat(@" FROM [{0}] WHERE Id = @id", entity.Name);

			return sqlStringBuilder.ToString();
		}
	}
}
