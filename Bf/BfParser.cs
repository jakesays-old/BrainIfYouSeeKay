using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;

namespace Std
{
	public class BfParser
	{
		private class Block
		{
			public Expression Body { get; private set; }

			private readonly List<Expression> _expressions;
			private readonly ParameterExpression _memParam;
			private readonly ParameterExpression _dataPtr;

			public LabelTarget BreakTarget { get; }

			public Block(ParameterExpression memParam, ParameterExpression dataPtr)
			{
				_memParam = memParam;
				_dataPtr = dataPtr;
				_expressions = new List<Expression>();
			}

			public Block(LabelTarget breakTarget, ParameterExpression memParam, ParameterExpression dataPtr)
				: this(memParam, dataPtr)
			{
				BreakTarget = breakTarget;
			}

			public void AddExpression(Expression expr)
			{
				_expressions.Add(expr);
			}

			public void Complete()
			{
				if (BreakTarget != null)
				{
					Body = Expression.Loop(
						Expression.Block(new [] 
						{
							Expression.IfThen(
								Expression.Equal(
									Expression.ArrayAccess(_memParam, _dataPtr),
									Expression.Constant(0)),
								Expression.Break(BreakTarget)
							)
						}.Concat(_expressions)
					),
					BreakTarget);
				}
				else
				{
					Body = Expression.Block(new[] {_dataPtr}, _expressions);
				}
			}
		}

		private static int _parseCounter;

		private MethodBuilder _debugMethod;
		private StringBuilder _debugText;
		private int _debugLineCounter;
		private Block _currentBlock;
		private Stack<Block> _blockStack;
		private SymbolDocumentInfo _debugSymbols;
		private TypeBuilder _debugType;
		private bool _debugEnabled;
		private string _debugTempPath;

        private void InitDebugging()
		{
			if (!_debugEnabled)
			{
				return;
			}

			_debugText = new StringBuilder();
			var instanceId = _parseCounter++;

			var name = $"BfAssembly{instanceId}";

			var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(
				new AssemblyName(name), AssemblyBuilderAccess.Run);
			var daType = typeof (DebuggableAttribute);
			var daCtor = daType.GetConstructor(new[] {typeof (DebuggableAttribute.DebuggingModes)});
			var daBuilder = new CustomAttributeBuilder(daCtor, new object[]
			{
				DebuggableAttribute.DebuggingModes.DisableOptimizations |
				DebuggableAttribute.DebuggingModes.Default
			});
			assembly.SetCustomAttribute(daBuilder);
			var module = assembly.DefineDynamicModule(name, true);
			_debugType = module.DefineType($"BfType{instanceId}", TypeAttributes.Public);

			//int[] memory, BfBuffer input, BfBuffer output
			_debugMethod = _debugType.DefineMethod("Execute",
				MethodAttributes.Public | MethodAttributes.Static,
				typeof (void), new[] {typeof (int[]), typeof (BfStream), typeof (BfStream) });
	        _debugTempPath = Path.Combine(_debugTempPath, $"BfSource{instanceId}X.bf");
            _debugSymbols = Expression.SymbolDocument(_debugTempPath);
		}

		const char IncDataPtr = '>';
		const char DecDataPtr = '<';
		const char IncCellValue = '+';
		const char DecCellValue = '-';
		const char OutputCellValue = '.';
		const char InputCellValue = ',';
		const char OpenLoop = '[';
		const char CloseLoop = ']';

		private int WriteDebugCommand(char cmd)
		{
			var currentPos = _debugText.Length;

			switch (cmd)
			{
				case IncDataPtr:
					_debugText.AppendLine("IncDataPtr");
					break;
				case DecDataPtr:
					_debugText.AppendLine("DecDataPtr");
					break;
				case IncCellValue:
					_debugText.AppendLine("IncCellValue");
					break;
				case DecCellValue:
					_debugText.AppendLine("DecCellValue");
					break;
				case OutputCellValue:
					_debugText.AppendLine("OutputCellValue");
					break;
				case InputCellValue:
					_debugText.AppendLine("InputCellValue");
					break;
				case OpenLoop:
					_debugText.AppendLine("OpenLoop");
					break;
				case CloseLoop:
					_debugText.AppendLine("CloseLoop");
					break;
			}

			return _debugText.Length - currentPos;
		}

		private void AddExpression(char cmd, Expression expression)
		{
			if (_debugEnabled)
			{
				var cmdLen = WriteDebugCommand(cmd);

				_debugLineCounter += 1;

				_currentBlock.AddExpression(
					Expression.DebugInfo(_debugSymbols, _debugLineCounter, 1, _debugLineCounter, cmdLen));
			}
			if (expression != null)
			{
				_currentBlock.AddExpression(expression);
			}
		}

		public BfProgram Parse(string source, string debugTempPath = null)
		{
			_debugTempPath = debugTempPath;
			_debugEnabled = debugTempPath != null;

			InitDebugging();

			var sourceData = source.ToCharArray();

			var srcPtr = 0;
			var srcLength = sourceData.Length;

			var memParam = Expression.Parameter(typeof (int[]), "memory");
			var inputParam = Expression.Parameter(typeof (BfStream), "input");
			var outputParam = Expression.Parameter(typeof (BfStream), "output");

			var dataPtr = Expression.Variable(typeof (int), "dataPtr");

			_currentBlock = new Block(memParam, dataPtr);
			_blockStack = new Stack<Block>();

			var writeMethod = typeof (BfStream).GetMethod("Put");
			var readMethod = typeof (BfStream).GetMethod("Put");

			while (srcPtr < srcLength)
			{
				var cmd = sourceData[srcPtr++];

				switch (cmd)
				{
					case IncDataPtr:
						AddExpression(cmd, Expression.AddAssign(dataPtr, Expression.Constant(1)));
						break;
					case DecDataPtr:
						AddExpression(cmd, Expression.SubtractAssign(dataPtr, Expression.Constant(1)));
						break;
					case IncCellValue:
						AddExpression(cmd, Expression.AddAssign(
							Expression.ArrayAccess(memParam, dataPtr),
							Expression.Constant(1)
							));
						break;
					case DecCellValue:
						AddExpression(cmd, Expression.SubtractAssign(
							Expression.ArrayAccess(memParam, dataPtr),
							Expression.Constant(1)
							));
						break;
					case OutputCellValue:
						AddExpression(cmd, Expression.Call(outputParam, writeMethod, 
							Expression.Convert(
								Expression.ArrayAccess(memParam, dataPtr), typeof(char)
								)
							)
						);
						break;
					case InputCellValue:
						AddExpression(cmd, Expression.Assign(
							Expression.ArrayAccess(memParam, dataPtr),
							Expression.Convert(
								Expression.Call(inputParam, readMethod), typeof (int)
								)
							)
						);
						break;
					case OpenLoop:
						_blockStack.Push(_currentBlock);
						_currentBlock = new Block(Expression.Label(), memParam, dataPtr);
						AddExpression(cmd, null);
						break;
					case CloseLoop:
						_currentBlock.Complete();
						var lastBlock = _currentBlock;
						_currentBlock = _blockStack.Pop();
						AddExpression(cmd, lastBlock.Body);
						break;

					default:
						break;
				}
			}

			_currentBlock.Complete();

			return CompileProgram(memParam, inputParam, outputParam);
		}

		private BfProgram CompileProgram(ParameterExpression memParam, 
			ParameterExpression inputParam,
			ParameterExpression outputParam)
		{
			var lambda = Expression.Lambda<BfProgram.BFunc>(_currentBlock.Body, memParam, inputParam, outputParam);

			if (_debugEnabled)
			{
				var generator = DebugInfoGenerator.CreatePdbGenerator();
				lambda.CompileToMethod(_debugMethod, generator);

				var completedType = _debugType.CreateType();
				File.WriteAllText(_debugTempPath, _debugText.ToString());

				return new BfProgram(completedType.GetMethod(_debugMethod.Name));
			}

			return new BfProgram(lambda.Compile());
		}
	}
}