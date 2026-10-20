// Translated from xsys35c 1.13.0 (https://github.com/kichikuou/xsys35c), decompiler/cali.c (parse, precedence,
// print_cali_prec) and the operator codes of common/common.h: Copyright (C) 2020 <KichikuouChrome@gmail.com>.
// GNU General Public License, version 2 or later, as KichikuouGodot (LICENSE).

namespace Kichikuou.Content.Decompiler;

/// <summary>A node of an expression ("cali") as xsys35dc reads it.</summary>
internal sealed class Cali(Cali.Kind type, int val, Cali? lhs = null, Cali? rhs = null)
{
    public enum Kind { Number, Variable, Op, ArrayRef }

    // Operators (common.h): the one-byte ones, and those after the 0xc0 prefix.
    public const byte OpAnd = 0x74, OpOr = 0x75, OpXor = 0x76, OpMul = 0x77, OpDiv = 0x78, OpAdd = 0x79,
        OpSub = 0x7a, OpEq = 0x7b, OpLt = 0x7c, OpGt = 0x7d, OpNe = 0x7e, OpEnd = 0x7f;
    public const byte OpC0Index = 1, OpC0Mod = 2, OpC0Le = 3, OpC0Ge = 4;

    public Kind Type { get; } = type;

    public int Val { get; set; } = val;

    /// <summary>The left operand; for an array reference, its index.</summary>
    public Cali? Lhs { get; } = lhs;

    public Cali? Rhs { get; } = rhs;
}

/// <summary>Reads and prints expressions (<c>cali.c</c>).</summary>
internal static class CaliCode
{
    /// <summary>
    /// Parses an expression at <paramref name="p"/> and moves past it (<c>parse_cali</c>). An assignment's
    /// left-hand side (<paramref name="isLhs"/>) is one operand, a variable or an array element.
    /// </summary>
    public static Cali Parse(byte[] code, ref int p, bool isLhs, IDecompileErrors errors)
    {
        var stack = new List<Cali>();
        do
        {
            byte op = code[p++];
            switch (op)
            {
                case Cali.OpEnd:
                    if (stack.Count == 0)
                        throw errors.At(p, "empty expression");
                    while (stack.Count >= 2)
                    {
                        // Rance 2, Oudou Yuusha
                        errors.Warning(p, "unexpected end of expression");
                        var r = Pop(stack);
                        var l = Pop(stack);
                        stack.Add(new Cali(Cali.Kind.Op, Cali.OpEnd, l, r));
                    }
                    return Pop(stack);

                case >= Cali.OpAnd and <= Cali.OpNe:
                {
                    if (stack.Count < 2)
                        throw errors.At(p, "stack underflow");
                    var rhs = Pop(stack);
                    var lhs = Pop(stack);
                    if (op == Cali.OpAdd && lhs is { Type: Cali.Kind.Number, Val: 16383 }
                        && rhs.Type == Cali.Kind.Number && rhs.Val <= 65535 - 16383)
                    {
                        lhs.Val += rhs.Val;
                        stack.Add(lhs);
                    }
                    else
                    {
                        stack.Add(new Cali(Cali.Kind.Op, op, lhs, rhs));
                    }
                    break;
                }

                case 0xc0:
                    op = code[p++];
                    if (op >= 0x40)
                    {
                        stack.Add(new Cali(Cali.Kind.Variable, op));
                        break;
                    }
                    switch (op)
                    {
                        case Cali.OpC0Index:
                        {
                            int var = code[p] << 8 | code[p + 1];
                            p += 2;
                            var index = Parse(code, ref p, false, errors);
                            stack.Add(new Cali(Cali.Kind.ArrayRef, var, index));
                            break;
                        }
                        case Cali.OpC0Mod:
                        case Cali.OpC0Le:
                        case Cali.OpC0Ge:
                        {
                            if (stack.Count < 2)
                                throw errors.At(p, "stack underflow");
                            var rhs = Pop(stack);
                            var lhs = Pop(stack);
                            stack.Add(new Cali(Cali.Kind.Op, op, lhs, rhs));
                            break;
                        }
                        default:
                            throw errors.At(p, $"unknown code c0 {op:x2}");
                    }
                    break;

                default:
                    if ((op & 0x80) != 0)
                    {
                        int var = op & 0x3f;
                        if (op > 0xc0)
                            var = var << 8 | code[p++];
                        stack.Add(new Cali(Cali.Kind.Variable, var));
                    }
                    else
                    {
                        int val = op & 0x3f;
                        if (op < 0x40)
                        {
                            val = val << 8 | code[p++];
                            if (val <= 0x33)
                                throw errors.At(p, $"unknown code 00 {val:x2}");
                        }
                        stack.Add(new Cali(Cali.Kind.Number, val));
                    }
                    break;
            }
        } while (!isLhs);

        if (stack.Count == 0)
            throw errors.At(p, "empty expression");
        if (stack.Count != 1)
            errors.Warning(p, "unexpected end of expression");
        var node = stack[^1];
        if (node.Type is not (Cali.Kind.Variable or Cali.Kind.ArrayRef))
            throw errors.At(p, $"unexpected left-hand-side for assignment {(int)node.Type}");
        return node;
    }

    /// <summary>
    /// Prints an expression with as few parentheses as the operators' precedence allows (<c>print_cali</c>). A
    /// variable without a name is named <c>VAR</c>n and the name is kept, as xsys35dc grows its list.
    /// </summary>
    public static void Print(Cali node, List<byte[]?> variables, DecompileOutput output) => Print(node, 0, variables, output);

    private static void Print(Cali node, int outPrec, List<byte[]?> variables, DecompileOutput output)
    {
        switch (node.Type)
        {
            case Cali.Kind.Number:
                output.Int(node.Val);
                break;

            case Cali.Kind.Variable:
            case Cali.Kind.ArrayRef:
                while (variables.Count <= node.Val)
                    variables.Add(null);
                variables[node.Val] ??= System.Text.Encoding.ASCII.GetBytes($"VAR{node.Val}");
                output.Bytes(variables[node.Val]!);
                if (node.Type == Cali.Kind.ArrayRef)
                {
                    output.Char('[');
                    Print(node.Lhs!, 0, variables, output);
                    output.Char(']');
                }
                break;

            case Cali.Kind.Op:
            {
                int prec = Precedence(node.Val);
                if (outPrec > prec)
                    output.Char('(');
                Print(node.Lhs!, prec, variables, output);
                output.Ascii(node.Val switch
                {
                    Cali.OpAnd => " & ",
                    Cali.OpOr => " | ",
                    Cali.OpXor => " ^ ",
                    Cali.OpMul => " * ",
                    Cali.OpDiv => " / ",
                    Cali.OpAdd => " + ",
                    Cali.OpSub => " - ",
                    Cali.OpEq => " = ",
                    Cali.OpLt => " < ",
                    Cali.OpGt => " > ",
                    Cali.OpNe => " \\ ",
                    Cali.OpC0Mod => " % ",
                    Cali.OpC0Le => " <= ",
                    Cali.OpC0Ge => " >= ",
                    Cali.OpEnd => " $ ",
                    _ => throw new InvalidOperationException($"BUG: unknown operator {node.Val}"),
                });
                Print(node.Rhs!, prec + 1, variables, output);
                if (outPrec > prec)
                    output.Char(')');
                break;
            }
        }
    }

    private static int Precedence(int op) => op switch
    {
        Cali.OpMul or Cali.OpDiv or Cali.OpC0Mod => 4,
        Cali.OpAdd or Cali.OpSub => 3,
        Cali.OpAnd or Cali.OpOr or Cali.OpXor => 2,
        Cali.OpLt or Cali.OpGt or Cali.OpC0Le or Cali.OpC0Ge => 1,
        Cali.OpEq or Cali.OpNe or Cali.OpEnd => 0,
        _ => throw new InvalidOperationException($"BUG: unknown operator {op}"),
    };

    private static Cali Pop(List<Cali> stack)
    {
        var node = stack[^1];
        stack.RemoveAt(stack.Count - 1);
        return node;
    }
}
