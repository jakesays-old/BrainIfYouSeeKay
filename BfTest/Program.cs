using System;
using System.Diagnostics;
using Std;

namespace BfTest
{
	class Program
	{
		static void Main(string[] args)
		{
			string debugTempPath = null;
			if (args.Length > 0)
			{
				debugTempPath = args[0];
			}

			var sw = Stopwatch.StartNew();
			var parser = new BfParser();
			var f = parser.Parse("+++++>+.>+.<<[->>[-<+>>+<]<[->+>+<<]>[-<+>]>[-<+>]<<.>.<<]",
				debugTempPath);

			const int iterationCounts = 1000;
			for (var i = 0; i < iterationCounts; i++)
			{
				var memory = new int[100000];
				var input = new BfMemoryStream("");
				var output = new BfMemoryStream(1024 * 128);
				f.Execute(memory, input, output);
			}
			sw.Stop();
			var speed = (double) sw.ElapsedTicks / iterationCounts;
			Console.WriteLine($"Perf {speed} ticks");
		}
	}
}
