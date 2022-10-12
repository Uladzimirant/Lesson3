using System.Security.AccessControl;
using System.Text.RegularExpressions;

namespace Lesson3
{
    //exception class for messages, cycle should restart on its catch
    class ExpectedException : Exception
    {
        public ExpectedException(string? message) : base(message)
        {
        }

        public ExpectedException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
    //info for operators and functions
    internal struct OpInfo
    {
        public byte Level; //Priority of operation
        public bool IsLeft; //Is operation left assosiated
        public byte RequiredAmount; //1 - Unary, 2 - Binary 
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
        //list of operators and functions, their info and lambdas
        static Dictionary<string, OpInfo> opInfo = new Dictionary<string, OpInfo>()
        {
            { "_" , new OpInfo(9, false, 1, (args) => { return -args[0]; }) },
            { "+", new OpInfo(2, true, 2, (args) => { return args[0] + args[1]; }) },
            { "-", new OpInfo(2, true, 2, (args) => { return args[0] - args[1]; }) },
            { "*", new OpInfo(3, true, 2, (args) => { return args[0] * args[1]; }) },
            { "/", new OpInfo(3, true, 2, (args) => { return args[0] / args[1]; }) },
            { "%", new OpInfo(3, true, 2, (args) => { return args[0] % args[1]; }) },
            { "^", new OpInfo(4, false, 2, (args) => { return Math.Pow(args[0], args[1]); }) },
            { "abs", new OpInfo(1, true, 1, (args) => {return Math.Abs(args[0]); }, true) },
            { "sin", new OpInfo(1, true, 1, (args) => {return Math.Sin(args[0]); }, true) },
            { "cos", new OpInfo(1, true, 1, (args) => {return Math.Cos(args[0]); }, true) },
            { "tan", new OpInfo(1, true, 1, (args) => {return Math.Tan(args[0]); }, true) },
            { "ctg", new OpInfo(1, true, 1, (args) => {return 1.0/Math.Tan(args[0]); }, true) },
            { "asin", new OpInfo(1, true, 1, (args) => {return Math.Asin(args[0]); }, true) },
            { "acos", new OpInfo(1, true, 1, (args) => {return Math.Acos(args[0]); }, true) },
            { "atan", new OpInfo(1, true, 1, (args) => {return Math.Atan(args[0]); }, true) },
            { "actg", new OpInfo(1, true, 1, (args) => {return Math.PI/2.0 - Math.Atan(args[0]); }, true) },
            { "sinh", new OpInfo(1, true, 1, (args) => {return Math.Sinh(args[0]); }, true) },
            { "cosh", new OpInfo(1, true, 1, (args) => {return Math.Cosh(args[0]); }, true) },
            { "tanh", new OpInfo(1, true, 1, (args) => {return Math.Tanh(args[0]); }, true) },
            { "ctgh", new OpInfo(1, true, 1, (args) => {return 1.0/Math.Tanh(args[0]); }, true) },
            { "asinh", new OpInfo(1, true, 1, (args) => {return Math.Asinh(args[0]); }, true) },
            { "acosh", new OpInfo(1, true, 1, (args) => {return Math.Acosh(args[0]); }, true) },
            { "atanh", new OpInfo(1, true, 1, (args) => {return Math.Atanh(args[0]); }, true) },
            { "sqrt", new OpInfo(1, true, 1, (args) => {return Math.Sqrt(args[0]); }, true) },
            { "cbrt", new OpInfo(1, true, 1, (args) => {return Math.Cbrt(args[0]); }, true) },
            { "exp", new OpInfo(1, true, 1, (args) => {return Math.Exp(args[0]); }, true) },
            { "ceiling", new OpInfo(1, true, 1, (args) => {return Math.Ceiling(args[0]); }, true) },
            { "floor", new OpInfo(1, true, 1, (args) => {return Math.Floor(args[0]); }, true) },
            { "round", new OpInfo(1, true, 1, (args) => {return Math.Round(args[0]); }, true) },
            { "log", new OpInfo(1, true, 1, (args) => {return Math.Log(args[0]); }, true) },
            { "logtwo", new OpInfo(1, true, 1, (args) => {return Math.Log2(args[0]); }, true) },
            { "logten", new OpInfo(1, true, 1, (args) => {return Math.Log10(args[0]); }, true) },
            { "sign", new OpInfo(1, true, 1, (args) => {return Math.Sign(args[0]); }, true) },
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
                    return opInfo.ContainsKey(s) ? (opInfo[s].IsFunction ? RPNType.FUNCTION : RPNType.OPERATION) : RPNType.OPERAND;
            }
        }

        //Converts our expression to Reverse Polish Notation by Shunting yard algorithm
        private static Queue<string> ToRPN(string expression)
        {
            //Usually stack used for output and expression is read from the end but it easier for me use queue 
            Queue<string> output = new Queue<string>();
            Stack<string> operatorStack = new Stack<string>();
            //For conventer
            expression = expression.Replace(".", ",");
            //To differentiate between unary and binary minus, we will replace unary minus with regex
            expression = Regex.Replace(expression, $"(?<=[({operators}])-", "_");
            //Regex Split, because String.Split() don't include separators
            var tokens = Regex.Split(expression, 
                $"([(){operators}]|{String.Join("|",functions.Select(e=>$"(?:{e})"))})" //Looks like this ([\\+\\-\\*\\/]|(?:f1)|(?:f2))
                );
            //Main algorithm written like in wikipedia, read "Shunting yard algorithm" for details
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
                            throw new ExpectedException("Expression not valid: parenthesis mismatch");
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
                if (op == "(") throw new ExpectedException("Expression not valid: parenthesis mismatch");
                output.Enqueue(op);
            }
            return output;
        }

        /* calculates result from reverse polish notation 
         * Algorithm:
         * while there is a token
         *   if operand then place in stack
         *   if operator or function then take nessesary amount for that operation from stack
         *      and place result in stack
         * get answer from one remaining element in stack
         */
        public static double calculateRPN(Queue<string> rpn)
        {
            Stack<double> stack = new Stack<double>();
            while (rpn.TryDequeue(out string elem))
            {
                switch (GetType(elem))
                {
                    case RPNType.FUNCTION:
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

        public static void Help()
        {
            Console.Write(
                "This is calculator based on Reverse Polish Notation\n" +
                "Write command or any valid expression to get result\n" +
                "For example 2+4.2*-(7+3^abs(-2))\n" +
                "Supported operators and functions:\n" +
                $"    {operators.Replace("\\", "").Replace("_","")} {String.Join(" ",functions)}\n" +
                $"Commands (if no command given input will be treated as expression):\n"+
                " help - this message\n" +
                " quit, exit - exit program\n" +
                " history [n] - get last n operations (default 5)\n"
                ) ;
        }
        static List<string> historyList = new List<string>();

        public static void History(int amount = 5)
        {
            if (amount < 1) throw new ExpectedException("Amount of elements in history must be positive");
            Console.WriteLine("History:");
            for (int i = Math.Max(historyList.Count - amount, 0); i < historyList.Count; i++)
            {
                Console.WriteLine(historyList[i]);
            }
        }
        public static void Main(string[] args)
        {
            bool running = true;
            Help();
            while (running)
            {
                try
                {
                    Console.Write("> ");
                    string fullLine = Console.ReadLine()?.Trim() ?? "";
                    string[] command = fullLine.Split(' ');
                    switch (command[0].ToLower())
                    {
                        case "quit":
                        case "exit":
                            running = false;
                            break;
                        case "help":
                            Help();
                            break;
                        case "history":
                            if (command.Length > 1)
                            {
                                try
                                {
                                    History(Convert.ToInt32(command[1]));
                                }
                                catch (FormatException) { throw new ExpectedException("History accepts integer number as argument"); }
                            }
                            else History();
                            break;
                        default:
                            double result = calculateRPN(ToRPN(fullLine));
                            Console.WriteLine(result);
                            historyList.Add($"{fullLine.Replace(" ", "")}={result}");
                            break;
                    }
                }
                catch (ExpectedException e)
                {
                    Console.WriteLine(e.Message);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }
    }
}