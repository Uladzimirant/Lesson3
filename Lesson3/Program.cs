using System.Security.AccessControl;
using System.Text.RegularExpressions;

namespace Lesson3
{
    class ExpectedException : Exception
    {
        public ExpectedException(string? message) : base(message)
        {
        }

        public ExpectedException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
    internal struct OpInfo
    {
        public byte Level;
        public bool IsLeft;
        public byte RequiredAmount;
        public Func<double[], double> Func;
        public bool IsFunction;

        public OpInfo(byte level, bool isLeft, byte requiredAmount, Func<double[], double> func, bool isFunction = false)
        {
            Level = level;
            IsLeft = isLeft;
            RequiredAmount = requiredAmount;
            Func = func;
            IsFunction = isFunction;
        }
    }
    public class Program
    {
        static Dictionary<string, OpInfo> opInfo = new Dictionary<string, OpInfo>()
        {
            { "_" , new OpInfo(1, true, 1, (args) => { return -args[0]; }) },
            { "+", new OpInfo(2, true, 2, (args) => { return args[0] + args[1]; }) },
            { "-", new OpInfo(2, true, 2, (args) => { return args[0] - args[1]; }) },
            { "*", new OpInfo(3, true, 2, (args) => { return args[0] * args[1]; }) },
            { "/", new OpInfo(3, true, 2, (args) => { return args[0] / args[1]; }) },
            { "^", new OpInfo(4, false, 2, (args) => { return Math.Pow(args[0], args[1]); }) },
            { "abs", new OpInfo(1, true, 1, (args) => {return Math.Abs(args[0]); }, true) }
        };
        static HashSet<string> functions = opInfo.Where(e => e.Value.IsFunction).Select(e => e.Key).ToHashSet();
        static string operators = String.Join("\\", opInfo.Where(e => !e.Value.IsFunction).Select(e => e.Key));
        enum RPNType
        {
            OPERAND,
            OPERATION,
            FUNCTION,
            LBRACKET,
            RBRACKET
        }
        static RPNType GetType(string s)
        {
            switch (s)
            {
                case "(": return RPNType.LBRACKET;
                case ")": return RPNType.RBRACKET;
                default:
                    return opInfo.ContainsKey(s) ? (opInfo[s].Level < 0 ? RPNType.FUNCTION : RPNType.OPERATION) : RPNType.OPERAND;
            }
        }
        private static Queue<string> ToRPN(string expression)
        {
            Queue<string> output = new Queue<string>();
            Stack<string> operatorStack = new Stack<string>();
            //To differentiate between unary and binary minus, we will replace unary minus with regex
            expression = Regex.Replace(expression, $"(?<=[({operators}])-", "_");
            Console.WriteLine(expression);
            //Regex Split, because String.Split() don't include separators
            var tokens = Regex.Split(expression, 
                $"([){operators}]|{String.Join("|",functions.Select(e=>$"(?:{e})"))})" //Looks like this ([\\+\\-\\*\\/]|(?:f1)|(?:f2))
                );
            //Main algorithm, read "Shunting yard algorithm" for details
            foreach (var token in tokens)
            {
                if (String.IsNullOrEmpty(token)) continue;
                switch (GetType(token))
                {
                    case RPNType.OPERATION:
                        while (operatorStack.TryPeek(out string op) &&
                            op != "(" &&
                            (opInfo[op].Level > opInfo[token].Level ||
                            opInfo[op].Level == opInfo[token].Level && opInfo[token].IsLeft)
                            )
                        {
                            output.Enqueue(operatorStack.Pop());
                        }
                        operatorStack.Push(token);
                        break;
                    case RPNType.FUNCTION:
                        operatorStack.Push(token);
                        break;
                    case RPNType.LBRACKET:
                        operatorStack.Push(token);
                        break;
                    case RPNType.RBRACKET:
                        try
                        {
                            while (operatorStack.Peek() != "(")
                            {
                                output.Enqueue(operatorStack.Pop());
                            }
                            if (operatorStack.Pop() != "(") throw new ArgumentException("Something wrong in expression");
                            if (functions.Contains(operatorStack.Peek()))
                            {
                                output.Enqueue(operatorStack.Pop());
                            }
                        } catch (InvalidOperationException)
                        {
                            throw new ArgumentException("Expression not valid: parenthesis mismatch");
                        }
                        break;
                    case RPNType.OPERAND:
                        output.Enqueue(token);
                        break;
                    default:
                        throw new Exception($"Shouldn't get in default, (ToRPN) {token}");
                }
            }
            while(operatorStack.TryPop(out string op))
            {
                if (op == "(") throw new ArgumentException("Expression not valid: parenthesis mismatch");
                output.Enqueue(op);
            }
            return output;
        }

        public static double calculateRPN(Queue<string> rpn)
        {
            Stack<double> stack = new Stack<double>();
            while (rpn.TryDequeue(out string elem))
            {
                switch (GetType(elem))
                {
                    case RPNType.OPERATION:
                        double[] args = new double[opInfo[elem].RequiredAmount];
                        for (int i = opInfo[elem].RequiredAmount - 1; i >=0 ; --i)
                        {
                            args[i] = stack.Pop();
                        }
                        stack.Push(opInfo[elem].Func(args));
                        break;
                    case RPNType.OPERAND:
                        try
                        {
                            stack.Push(Convert.ToDouble(elem));
                        } catch (FormatException e) { throw new ExpectedException($"{elem} is not a number"); }
                        break;
                    default:
                        throw new Exception($"Shouldn't get in default, (calculateRPN) {elem}");
                }
            }

            return stack.Pop();
        }

        public static void Main(string[] args)
        {
            string str = Console.ReadLine();
            var st = ToRPN(str);
            Console.WriteLine(calculateRPN(st));
        }
    }
}