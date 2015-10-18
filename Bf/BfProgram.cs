using System.Reflection;

namespace Std
{
	public class BfProgram
	{
		public delegate void BFunc(int[] memory, BfStream input, BfStream output);

		private readonly MethodInfo _debugMethod;
		private readonly BFunc _method;

		public BfProgram(BFunc method)
		{
			_method = method;
		}

		public BfProgram(MethodInfo debugMethod)
		{
			_debugMethod = debugMethod;
		}

		public void Execute(int[] memory, BfStream input, BfStream output)
		{
			if (_debugMethod != null)
			{
				_debugMethod.Invoke(null, new object[]
				{
					memory,
					input,
					output
				});
			}
			else
			{
				_method(memory, input, output);
			}
		}
	}
}