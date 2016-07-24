using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MSBuilder
{
	class Program
	{
		public static void Main(string[] args)
		{
			var properties = new Dictionary<string, string>();
			var projectFile = default(string);

			if (args.Length == 0)
			{
				// We'll get everything from input stream
				using (var input = Console.OpenStandardInput())
				{
					var element = XElement.Load(input);
					foreach (var child in element.Elements())
					{
						var propertyName = child.Attribute("Name").Value;
						if (propertyName.ToUpperInvariant() == "PROJECTFILE")
							projectFile = child.Value.Trim();
						else
							properties[propertyName] = child.Value.Trim();
					}
				}
			}
			else
			{
				projectFile = args[0];
			}

			ProjectReader.Read(projectFile, properties)
				.Save(Console.Out);
		}
	}
}
