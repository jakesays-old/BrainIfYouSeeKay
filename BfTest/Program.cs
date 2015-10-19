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

			var parser = new BfParser();
			var f = parser.Parse("+++++>+.>+.<<[->>[-<+>>+<]<[->+>+<<]>[-<+>]>[-<+>]<<.>.<<]",
				debugTempPath);

			var memory = new int[100000];
			var input = new BfMemoryStream("");
			var output = new BfMemoryStream(1024 * 128);

			var sw = Stopwatch.StartNew();
			const int iterationCounts = 1000;
			for (var i = 0; i < iterationCounts; i++)
			{
				f.Execute(memory, input, output);
				Array.Clear(memory, 0, memory.Length);
				output.Reset();
			}
			sw.Stop();
			var speed = (double) sw.ElapsedTicks / iterationCounts;
			Console.WriteLine($"Perf {speed} ticks");
		}
	}
}
