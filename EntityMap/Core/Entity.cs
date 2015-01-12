using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EntityMap.Core {
	public class Entity : ICloneable {
		public string Name { get; set; }
		public Dictionary<string, ValueProperty> Properties { get; set; }
		private Dictionary<string, Property> _internalProperties;

		public Entity () {
			this.Properties = new Dictionary<string, ValueProperty>(StringComparer.OrdinalIgnoreCase);
		}

		public object this[string propertyName] {
			get {
				return this.Properties[propertyName].Value;
			}
			set {
				Property property = this._internalProperties[propertyName];
				this.Properties[propertyName] = new ValueProperty(propertyName, value, property.DataType, property.ProviderSpecificDataType, property.ProviderType, property.Ordinal, property.Size, property.AllowNull);
			}
		}

		public Entity (string entityName, IEnumerable<Property> properties) {
			this.Name = entityName;
			this.Properties = new Dictionary<string, ValueProperty>(StringComparer.OrdinalIgnoreCase);
			this._internalProperties = new Dictionary<string, Property>(StringComparer.OrdinalIgnoreCase);

			foreach (Property property in properties) {
				this._internalProperties.Add(property.Name, property);
			}
		}

		public Dictionary<string, Property> GetInternalProperties () {
			return _internalProperties;
		}

		public bool Contains (string propertyName) {
			return this.Properties.ContainsKey(propertyName);
		}

		public T GetValue<T> (string propertyName) {
			object obj = this.Properties[propertyName].Value;
			return (T)obj;
		}

		public void SetValue (string propertyName, object value) {
			Property property = this._internalProperties[propertyName];
			this.Properties[propertyName] = new ValueProperty(propertyName, value, property.DataType, property.ProviderSpecificDataType, property.ProviderType, property.Ordinal, property.Size, property.AllowNull);
		}

		public object Clone () {
			return new Entity(this.Name, this._internalProperties.Values);
		}
	}
}
