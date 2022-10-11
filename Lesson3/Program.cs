using System.Security.AccessControl;
using System.Text.RegularExpressions;

namespace Lesson3
{ 
    public class Program
    {
        static Dictionary<string, byte> level = new Dictionary<string, byte>()
        {
            { "+", 2 },
            { "-", 2 },
            { "*", 3 },
            { "/", 3 },
            { "^", 4 }
        };
        static Dictionary<string, bool> isLeft = new Dictionary<string, bool>()
        {
            { "+", true },
            { "-", true },
            { "*", true },
            { "/", true },
            { "^", false }
        };
        private static Stack<string> ToRPN(string expression)
        {
            Stack<string> outputStack = new Stack<string>();
            Stack<string> operatorStack = new Stack<string>();
            var tokens = Regex.Split(expression, "([+/*()%!^-])"); //Регулярный Split, потому что обычный String.Split() не включает разделители в итог
            foreach (var token in tokens)
            {
                if (String.IsNullOrEmpty(token)) continue;
                switch (token)
                {
                    case "+":
                    case "-":
                    case "*":
                    case "/":
                    case "%":
                    case "^":
                        while (operatorStack.TryPeek(out string op) &&
                            op != "(" &&
                            (level[op] > level[token] || level[op] == level[token] && isLeft[token])
                            )
                        {
                            outputStack.Push(operatorStack.Pop());
                        }
                        operatorStack.Push(token);
                        break;
                    case "(":
                        operatorStack.Push(token);
                        break;
                    case ")":
                        try
                        {
                            while (operatorStack.Peek() != "(")
                            {
                                outputStack.Push(operatorStack.Pop());
                            }
                            if (operatorStack.Pop() != "(") throw new ArgumentException("Something wrong in expression");
                        } catch (InvalidOperationException)
                        {
                            throw new ArgumentException("Expression not valid: parenthesis mismatch");
                        }
                        break;
                    default:
                        outputStack.Push(token);
                        break;
                }
            }
            while(operatorStack.TryPop(out string op))
            {
                //if (op == "(") throw new ArgumentException("Expression not valid: parenthesis mismatch");
                outputStack.Push(op);
            }
            return outputStack;
        }
        public static void Main(string[] args)
        {
            string str = Console.ReadLine();
            foreach (var t in ToRPN(str))
            {
                Console.WriteLine(t);
            }
        }
    }
}