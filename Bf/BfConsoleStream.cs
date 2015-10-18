using System;

namespace Std
{
	class BfConsoleStream : BfStream
	{
		public override char Get()
		{
			return (char) Console.Read();
		}

		public override void Put(char c)
		{
			Console.Write(c);
		}
	}
}