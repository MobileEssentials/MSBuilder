using Microsoft.Build.Construction;
using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace MSBuilder
{
	public class IntegrationTest : TaskInlinerTest, IDisposable
	{
		readonly ITestOutputHelper writer;

		public IntegrationTest(ITestOutputHelper writer)
		{
			this.writer = writer;
		}

		public void Dispose()
		{
		}

		[InlineData(true)]
		[InlineData(false)]
		[Theory]
		public void when_building_then_succeeds(bool useCompiledTasks)
		{
			Build(useCompiledTasks, buildTarget => 
			{
				buildTarget.DependsOnTargets = "IntrospectProperties;IntrospectTargets";
			}, 
			@"..\..\..\Introspect\bin\MSBuilder.Introspect.targets");
		}
	}
}