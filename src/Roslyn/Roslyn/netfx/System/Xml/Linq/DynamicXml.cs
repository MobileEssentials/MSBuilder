#region BSD License
/* 
Copyright (c) 2010, NETFx
All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.

* Neither the name of Clarius Consulting nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.Collections;

namespace System.Xml.Linq
{
	/// <summary>
	/// Provides the <see cref="ToDynamic"/> extension method for 
	/// <see cref="XElement"/>, allowing read-only dynamic API
	/// access over the underlying XML.
	/// </summary>
	internal static class DynamicXmlExtensions
	{
		private static Dictionary<Type, Func<string, object>> xmlConverters;

		static DynamicXmlExtensions()
		{
			xmlConverters = new Dictionary<Type, Func<string, object>>
			{
				{ typeof(Boolean), s => XmlConvert.ToBoolean(s) },
				{ typeof(Byte), s => XmlConvert.ToByte(s) },
				{ typeof(Char), s => XmlConvert.ToChar(s) },
				{ typeof(DateTime), s => XmlConvert.ToDateTime(s, XmlDateTimeSerializationMode.RoundtripKind) },
				{ typeof(DateTimeOffset), s => XmlConvert.ToDateTimeOffset(s) },
				{ typeof(Decimal), s => XmlConvert.ToDecimal(s) },
				{ typeof(Double), s => XmlConvert.ToDouble(s) },
				{ typeof(Guid), s => XmlConvert.ToGuid(s) },
				{ typeof(Int16), s => XmlConvert.ToInt16(s) },
				{ typeof(Int32), s => XmlConvert.ToInt32(s) },
				{ typeof(Int64), s => XmlConvert.ToInt64(s) },
				{ typeof(SByte), s => XmlConvert.ToSByte(s) },
				{ typeof(Single), s => XmlConvert.ToSingle(s) },
				{ typeof(TimeSpan), s => XmlConvert.ToTimeSpan(s) },
				{ typeof(UInt16), s => XmlConvert.ToUInt16(s) },
				{ typeof(UInt32), s => XmlConvert.ToUInt32(s) },
				{ typeof(UInt64), s => XmlConvert.ToUInt64(s) },
			};
		}

		/// <summary>
		/// Converts the element into a dynamic object to use 
		/// dotted and indexer notation for elements and attribtes, 
		/// with built-in support for <see cref="XmlConvert"/> when 
		/// casting the resulting values.
		/// </summary>
		/// <nuget id="netfx-System.Xml.DynamicXml" />
		/// <param name="xml" this="true">The xelement to access dinamically</param>
		public static dynamic ToDynamic(this XElement xml)
		{
			return new DynamicXmlElement(xml);
		}

		private static bool TryXmlConvert(string value, Type returnType, out object result)
		{
			if (returnType == typeof(string))
			{
				result = value;
				return true;
			}
			else if (returnType.IsEnum)
			{
				// First try enum try parse:
				if (Enum.IsDefined(returnType, value))
				{
					result = Enum.Parse(returnType, value);
					return true;
				}

				// We know we support all underlying types for enums, 
				// which are all numeric.
				var enumType = Enum.GetUnderlyingType(returnType);
				var rawValue = xmlConverters[enumType].Invoke(value);

				result = Enum.ToObject(returnType, rawValue);
				return true;
			}
			else
			{
				var converter = default(Func<string, object>);
				if (xmlConverters.TryGetValue(returnType, out converter))
				{
					result = converter(value);
					return true;
				}
			}

			result = null;
			return false;
		}

		private class DynamicXmlElement : DynamicObject, IEnumerable<XElement>, IEnumerable<DynamicObject>
		{
			private XElement xml;

			public DynamicXmlElement(XElement xml)
			{
				this.xml = xml;
			}

			public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
			{
				var name = GetNameIndex(indexes);
				var existingAttr = this.xml.Attribute(name);
				if (existingAttr == null)
				{
					// Try element name.
					var matches = this.xml.Elements(name);

					// If we have more than one, return the collection.
					if (matches.Skip(1).Any())
					{
						result = new DynamicXmlElements(matches);
					}
					else
					{
						result = matches
							.Select(x => new DynamicXmlElement(x))
							.FirstOrDefault();
					}

					return true;
				}
				else
				{
					result = new DynamicXmlAttribute(existingAttr);
					return true;
				}
			}

			public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
			{
				var name = GetNameIndex(indexes);
				var existingAttr = this.xml.Attribute(name);
				var existingEl = this.xml.Element(name);

				var attrValue = value as XAttribute;
				var stringValue = value as string;

				if (existingAttr == null)
				{
					if (existingEl != null)
					{
						existingEl.SetValue(value);
					}
					else
					{
						if (stringValue != null)
							attrValue = new XAttribute(name, value);

						this.xml.Add(attrValue);
					}
				}
				else
				{
					if (attrValue != null)
						stringValue = attrValue.Value;

					existingAttr.SetValue(stringValue);
				}

				return true;
			}

			private XName GetNameIndex(object[] indexes)
			{
				if (indexes.Length != 1)
					throw new NotSupportedException("Attributes can only be accessed using a single index of type string or XName");

				var result = default(XName);
				if (indexes[0] is string)
					result = XName.Get((string)indexes[0]);
				else if (indexes[0] is XName)
					result = (XName)indexes[0];
				else
					throw new NotSupportedException("Attribute index can only be a simple attribute name, an expanded XML name string, or an XName");

				return result;
			}

			public override bool TryGetMember(GetMemberBinder binder, out object result)
			{
				var matches = this.xml.Elements().Where(x => x.Name.LocalName.Equals(binder.Name));

				// If we have more than one, return the collection.
				if (matches.Skip(1).Any())
				{
					result = new DynamicXmlElements(matches);
				}
				else
				{
					result = matches
						.Select(x => new DynamicXmlElement(x))
						.FirstOrDefault();
				}

				return true;
			}

			public override bool TryConvert(ConvertBinder binder, out object result)
			{
				if (binder.ReturnType == typeof(XElement))
				{
					result = this.xml;
					return true;
				}
				else if (binder.ReturnType == typeof(IEnumerable))
				{
					result = this.xml.Elements().Select(el => new DynamicXmlElement(el)).ToList();
					return true;
				}
				else if (TryXmlConvert(this.xml.Value, binder.ReturnType, out result))
				{
					return true;
				}

				return base.TryConvert(binder, out result);
			}

			public IEnumerator<XElement> GetEnumerator()
			{
				return this.xml.Elements().GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return this.GetEnumerator();
			}

			IEnumerator<DynamicObject> IEnumerable<DynamicObject>.GetEnumerator()
			{
				return this.xml.Elements().Select(el => new DynamicXmlElement(el)).GetEnumerator();
			}
		}

		private class DynamicXmlAttribute : DynamicObject
		{
			private XAttribute xml;

			public DynamicXmlAttribute(XAttribute xml)
			{
				this.xml = xml;
			}

			public override bool TryConvert(ConvertBinder binder, out object result)
			{
				if (binder.ReturnType == typeof(XAttribute))
				{
					result = this.xml;
					return true;
				}
				else if (TryXmlConvert(this.xml.Value, binder.ReturnType, out result))
				{
					return true;
				}

				return base.TryConvert(binder, out result);
			}

			public override string ToString()
			{
				return this.xml.Value;
			}
		}

		private class DynamicXmlElements : DynamicObject, IEnumerable<XElement>, IEnumerable<DynamicObject>
		{
			private List<XElement> elements;

			public DynamicXmlElements(IEnumerable<XElement> elements)
			{
				this.elements = elements.ToList();
			}

			public override bool TryConvert(ConvertBinder binder, out object result)
			{
				if (binder.ReturnType.IsAssignableFrom(typeof(IEnumerable)) ||
					binder.ReturnType.IsAssignableFrom(typeof(IEnumerable<object>)) ||
					binder.ReturnType.IsAssignableFrom(typeof(IEnumerable<DynamicObject>)))
				{
					result = elements.Select(el => new DynamicXmlElement(el)).ToList();
					return true;
				}
				else if (binder.ReturnType == typeof(object[]))
				{
					result = elements.Select(el => (object)new DynamicXmlElement(el)).ToArray();
					return true;
				}

				return base.TryConvert(binder, out result);
			}

			public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
			{
				if (indexes.Length == 1 && indexes[0] is int)
				{
					result = new DynamicXmlElement(this.elements[(int)indexes[0]]);
					return true;
				}

				return base.TryGetIndex(binder, indexes, out result);
			}

			public IEnumerator<XElement> GetEnumerator()
			{
				return this.elements.GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				// Untyped enumeration goes through the dynamic route.
				return ((IEnumerable<DynamicObject>)this).GetEnumerator();
			}

			IEnumerator<DynamicObject> IEnumerable<DynamicObject>.GetEnumerator()
			{
				return this.elements.Select(el => new DynamicXmlElement(el)).GetEnumerator();
			}
		}
	}
}