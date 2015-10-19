using System;

namespace Std
{
	public class BfMemoryStream : BfStream
	{
		private readonly char[] _buffer;
		private int _bufferIndex;

		private enum Type
		{
			Read,
			Write
		}

		private readonly Type _bufferType;

		public BfMemoryStream(string input)
		{
			_bufferType = Type.Read;
			_buffer = input.ToCharArray();
		}

		public BfMemoryStream(int bufferSize)
		{
			_bufferType = Type.Write;
			_buffer = new char[bufferSize];
		}

		private void Check(Type expectedType)
		{
			if (_bufferType != expectedType)
			{
				throw new InvalidOperationException($"BfMemoryStream in state {_bufferType}, expected state {expectedType}");
			}
		}

		public string Output
		{
			get
			{
				Check(Type.Write);
				return new string(_buffer, 0, _bufferIndex);
			}
		}

		public void Reset()
		{
			_bufferIndex = 0;
		}

		public override char Get()
		{
			Check(Type.Read);
			return _buffer[_bufferIndex++];
		}

		public override void Put(char c)
		{
			Check(Type.Write);
			_buffer[_bufferIndex++] = c;
		}
	}
}