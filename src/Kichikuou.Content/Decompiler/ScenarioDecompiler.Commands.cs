// Translated from xsys35c 1.13.0 (https://github.com/kichikuou/xsys35c), decompiler/decompile.c (get_command,
// arguments and the command switch of decompile_page) and the command codes of common/common.h: Copyright (C) 2020
// <KichikuouChrome@gmail.com>. GNU General Public License, version 2 or later, as KichikuouGodot (LICENSE).

namespace Kichikuou.Content.Decompiler;

internal sealed partial class ScoDecompiler
{
    private static int Cmd2(char a, char b) => a | b << 8;

    private static int Cmd2F(int b) => 0x2f | b << 8;

    /// <summary>
    /// The commands whose arguments follow one signature (<c>arguments</c>): <c>e</c> an expression, <c>v</c> a
    /// variable, <c>n</c> a byte, <c>N</c> a byte with no space before it, <c>s</c> a string ending in <c>:</c>,
    /// <c>z</c> one ending in NUL, <c>o</c> an obfuscated string, <c>F</c> a function. The mnemonic is the
    /// command's bytes (1-3 letters), the key its code (<c>CMD2</c>, <c>CMD3</c>).
    /// </summary>
    private static readonly string[] Signatures =
    [
        "CB eeeee", "CC eeeeee", "CD eeeeeeeeee", "CE eeeeeeeee", "CF eeeee", "CK neeeeeeee", "CL eeeee",
        "CM eeeeeeeee", "CP eee", "CS eeeeeee", "CT vee", "CU eeeeee", "CV eeeeee", "CX eeeeeeee", "CY eeeee",
        "CZ eeeeeee",
        "DC eee", "DF vee", "DI evv", "DR v", "DS vvee",
        "EC e", "EG evvvv", "EM evee", "EN veeee", "ES eeeeee",
        "F Nee",
        "GS ev", "GX ee",
        "H ne",
        "IC ev", "IE ee", "IG veee", "IK n", "IM vv", "IX v", "IY e", "IZ ee",
        "KI vee", "KK e", "KN v", "KP v", "KQ ve", "KR v", "KW ve",
        "LC ees", "LD e", "LE nsee", "LHD ne", "LHG ne", "LHM ne", "LHS ne", "LHW ne", "LXC e", "LXG ess",
        "LXL eee", "LXO eee", "LXP eee", "LXR eve", "LXS evv", "LXW eve", "LXX eev", "LL neee", "LP eve", "LT ev",
        "MA ee", "MC ee", "MD eee", "ME eeeee", "MF veee", "MG ne", "MH eee", "MI ees", "MJ eeeee", "ML ve",
        "MM ee", "MN nev", "MP ee", "MS es", "MT s", "MV e", "MZ neee",
        "N+ vee", "N- vee", "N* vee", "N/ vee", "N> veev", "N< veev", "N= veev", "N\\ ve", "N& vev", "N| vev",
        "N^ vev", "N~ ve", "NB vve", "NC ve", "NI veee", "NO nvve", "NP vvev", "NR ev", "NT s", "ND+ eee",
        "ND- eee", "ND* eee", "ND/ eee", "NDA ee", "NDC ee", "NDD ve", "NDH ee", "NDM ee",
        "PC e", "PD e", "PG vee", "PN e", "PP vee", "PS eeee",
        "QC ee", "QD e", "QE nsee", "QP eve",
        "SC v", "SI nv", "SL e", "SM e", "SO v", "SP ee", "SQ eee", "SS e", "ST e", "SU vv", "SV ee", "SW veee",
        "T ee",
        "UC ne", "UD e", "UG ee", "UR v", "US ee",
        "VA neee", "VB eeeeeee", "VC eeeeeee", "VE eeeeee", "VF ", "VG eeee", "VH eeeeee", "VIC eeee",
        "VIP eeee", "VJ eeee", "VP eeeeee", "VR eev", "VS eeeee", "VT eeeeeeeeee", "VV ee", "VW eev", "VX eeee",
        "VZ nee",
        "WV eeee", "WW eee", "WX eeee", "WZ ne",
        "X e", "Y ee",
        "ZA ne", "ZB e", "ZC ee", "ZD ne", "ZE e", "ZF e", "ZG v", "ZH e", "ZI ee", "ZK ees", "ZL e", "ZM e",
        "ZR ev", "ZS e", "ZW e", "ZZ Ne",
    ];

    /// <summary>
    /// The commands <c>2F nn</c> by <c>nn</c>: the name xsys35dc prints and the signature. A null name prints
    /// nothing; a null signature is a command decoded by hand (<see cref="Command"/>).
    /// </summary>
    private static readonly (string? Name, string? Signature)[] SlashCommands =
    [
        ("TOC", ""), ("TOS", ""), ("TPC", "e"), ("TPS", "e"), ("TOP", ""), ("TPP", ""), ("inc", "v"), ("dec", "v"),
        ("TAA", "e"), ("TAB", "v"), ("wavLoad", "ee"), ("wavPlay", "ee"), ("wavStop", "e"), ("wavUnload", "e"),
        ("wavIsPlay", "ev"), ("wavFade", "eeee"),
        // 0x10
        ("wavIsFade", "ev"), ("wavStopFade", "e"), ("trace", "z"), ("wav3DSetPos", "eeee"), ("wav3DCommit", ""),
        ("wav3DGetPos", "evvv"), ("wav3DSetPosL", "eee"), ("wav3DGetPosL", "vvv"), ("wav3DFadePos", "eeeee"),
        ("wav3DIsFadePos", "ev"), ("wav3DStopFadePos", "e"), ("wav3DFadePosL", "eeee"), ("wav3DIsFadePosL", "v"),
        ("wav3DStopFadePosL", ""), ("sndPlay", "ee"), ("sndStop", ""),
        // 0x20
        ("sndIsPlay", "v"), (null, null) /* msg */, ("HH", "ne"), ("LC", "eez"), ("LE", "nzee"), ("LXG", "ezz"),
        ("MI", "eez"), ("MS", "ez"), ("MT", "z"), ("NT", "z"), ("QE", "nzee"), ("UP", null), ("F", "Nee"),
        ("wavWaitTime", "ee"), ("wavGetPlayPos", "ev"), ("wavWaitEnd", "e"),
        // 0x30
        ("wavGetWaveTime", "ev"), ("menuSetCbkSelect", "F"), ("menuSetCbkCancel", "F"), ("menuClearCbkSelect", ""),
        ("menuClearCbkCancel", ""), ("wav3DSetMode", "ee"), ("grCopyStretch", "eeeeeeeee"), ("grFilterRect", "eeeee"),
        ("iptClearWheelCount", ""), ("iptGetWheelCount", "vv"), ("menuGetFontSize", "v"), ("msgGetFontSize", "v"),
        ("strGetCharType", "eev"), ("strGetLengthASCII", "ev"), ("sysWinMsgLock", ""), ("sysWinMsgUnlock", ""),
        // 0x40
        ("aryCmpCount", "veev"), ("aryCmpTrans", "veeeev"), ("grBlendColorRect", "eeeeeeeee"),
        ("grDrawFillCircle", "eeee"), ("MHH", "eee"), ("menuSetCbkInit", "F"), ("menuClearCbkInit", ""),
        (null, null) /* menu */, ("sysOpenShell", "z"), ("sysAddWebMenu", "zz"), ("iptSetMoveCursorTime", "e"),
        ("iptGetMoveCursorTime", "v"), ("grBlt", "eeeeee"), ("LXWT", "ez"), ("LXWS", "ee"), ("LXWE", "ee"),
        // 0x50
        ("LXWH", "ene"), ("LXWHH", "ene"), ("sysGetOSName", "e"), ("patchEC", "e"), ("mathSetClipWindow", "eeee"),
        ("mathClip", "vvvvvv"), ("LXF", "ezz"), ("strInputDlg", "zeev"), ("strCheckASCII", "ev"),
        ("strCheckSJIS", "ev"), ("strMessageBox", "z"), ("strMessageBoxStr", "e"), ("grCopyUseAMapUseA", "eeeeeee"),
        ("grSetCEParam", "ee"), ("grEffectMoveView", "eeee"), ("cgSetCacheSize", "e"),
        // 0x60
        (null, null) /* dllCall */, ("gaijiSet", "ee"), ("gaijiClearAll", ""), ("menuGetLatestSelect", "v"),
        ("lnkIsLink", "eev"), ("lnkIsData", "eev"), ("fncSetTable", "eF"), ("fncSetTableFromStr", "eev"),
        ("fncClearTable", "e"), ("fncCall", "e"), ("fncSetReturnCode", "e"), ("fncGetReturnCode", "v"),
        ("msgSetOutputFlag", "e"), ("saveDeleteFile", "ev"), ("wav3DSetUseFlag", "e"), ("wavFadeVolume", "eeee"),
        // 0x70
        ("patchEMEN", "e"), ("wmenuEnableMsgSkip", "e"), ("winGetFlipFlag", "v"), ("cdGetMaxTrack", "v"),
        ("dlgErrorOkCancel", "zv"), ("menuReduce", "e"), ("menuGetNumof", "v"), ("menuGetText", "ee"),
        ("menuGoto", "ee"), ("menuReturnGoto", "ee"), ("menuFreeShelterDIB", ""), ("msgFreeShelterDIB", ""),
        (null, null) /* ainMsg */, (null, null) /* ainH */, (null, null) /* ainHH */, (null, null) /* ainX */,
        // 0x80
        ("dataSetPointer", "F"), ("dataGetWORD", "ve"), ("dataGetString", "ee"), ("dataSkipWORD", "e"),
        ("dataSkipString", "e"), ("varGetNumof", "v"), ("patchG0", "e"), ("regReadString", "eeev"),
        ("fileCheckExist", "ev"), ("timeCheckCurDate", "eeev"), ("dlgManualProtect", "oo"),
        ("fileCheckDVD", "oeeov"), ("sysReset", ""),
    ];

    private static readonly Dictionary<int, string> SignatureOf = BuildSignatures();

    private static Dictionary<int, string> BuildSignatures()
    {
        var table = new Dictionary<int, string>();
        foreach (string line in Signatures)
        {
            int space = line.IndexOf(' ');
            string mnemonic = line[..space];
            int code = 0;
            for (int i = 0; i < mnemonic.Length; i++)
                code |= mnemonic[i] << (8 * i);
            table.Add(code, line[(space + 1)..]);
        }
        for (int sub = 0; sub < SlashCommands.Length; sub++)
            if (SlashCommands[sub].Signature is { } signature)
                table.Add(Cmd2F(sub), signature);
        return table;
    }

    /// <summary><c>get_command</c>: reads a command's code, printing its mnemonic.</summary>
    private int GetCommand()
    {
        byte[] d = Current.Data;
        switch (d[_p])
        {
            case (byte)'/':
            {
                _p++;
                int sub = d[_p++];
                if (sub >= SlashCommands.Length)
                    throw At(_p - 2, $"Unsupported command 2f {sub:x2}");
                if (sub == 0x47)
                {
                    if (d[_p] != ']')
                        throw At(_p - 2, "command 2F47 not followed by ']'");
                    Putc(d[_p++]);
                    return Cmd2F(sub);
                }
                if (SlashCommands[sub].Name is { } name)
                    Puts(name);
                return Cmd2F(sub);
            }
            case (byte)'G':
                return d[_p + 1] is (byte)'S' or (byte)'X' ? Command2() : Command1();
            case (byte)'N':
                return d[_p + 1] == 'D' ? Command3() : Command2();
            case (byte)'V':
                return d[_p + 1] == 'I' ? Command3() : Command2();
            case (byte)'L':
                return d[_p + 1] is (byte)'H' or (byte)'X' ? Command3() : Command2();
            case (byte)'C' or (byte)'D' or (byte)'E' or (byte)'I' or (byte)'K' or (byte)'M' or (byte)'P' or (byte)'Q'
                or (byte)'S' or (byte)'U' or (byte)'W' or (byte)'Z':
                return Command2();
            case >= 0x10 and <= 0x17:
                Putc('!');
                return d[_p++];
            default:
                return Command1();
        }

        int Command1()
        {
            Putc(d[_p]);
            return d[_p++];
        }

        int Command2()
        {
            Putc(d[_p]);
            Putc(d[_p + 1]);
            int code = d[_p] | d[_p + 1] << 8;
            _p += 2;
            return code;
        }

        int Command3()
        {
            Putc(d[_p]);
            Putc(d[_p + 1]);
            Putc(d[_p + 2]);
            int code = d[_p] | d[_p + 1] << 8 | d[_p + 2] << 16;
            _p += 3;
            return code;
        }
    }

    /// <summary><c>arguments</c>: a command's arguments by their signature, and the closing <c>:</c>.</summary>
    private void Arguments(string sig)
    {
        byte[] d = Current.Data;
        string sep = " ";
        foreach (char c in sig)
        {
            if (c != 'N')
                Puts(sep);
            sep = ", ";

            switch (c)
            {
                case 'e':
                case 'v':
                    Expr(false);
                    break;
                case 'n':
                case 'N':
                {
                    int n = d[_p++];
                    _out?.Int(n);
                    break;
                }
                case 's':
                case 'z':
                    // S380, newer than S360: quoted and escaped.
                    Putc('"');
                    _p = PutString(d, _p, c == 'z' ? (byte)0 : (byte)':', StringEscape);
                    Putc('"');
                    break;
                case 'o': // obfuscated string
                {
                    if (d[_p] != 0)
                        throw At(_p, "0x00 expected");
                    Putc('"');
                    _p++;
                    int end = Array.IndexOf(d, (byte)0, _p);
                    var buf = new byte[end - _p + 1];
                    for (int i = 0; i < buf.Length - 1; i++)
                        buf[i] = (byte)(d[_p + i] >> 4 | d[_p + i] << 4);
                    PutString(buf, 0, 0, StringEscape);
                    _p = end + 1;
                    Putc('"');
                    break;
                }
                case 'F':
                    throw At(_p, "a command taking a function is not supported");
                default:
                    throw new InvalidOperationException($"BUG: invalid arguments() template : {c}");
            }
        }
        Putc(':');
    }

    /// <summary>
    /// The command switch of <c>decompile_page</c>: a command's arguments after its code; false for a command or a
    /// subcommand xsys35dc does not know (<c>unknown_command</c>).
    /// </summary>
    private bool Command(int cmd, List<uint> branchEnd, ref bool inMenuItem)
    {
        switch (cmd)
        {
            case '!': // Assign
            case >= 0x10 and <= 0x17:
            {
                Expr(true);
                Putc(' ');
                if (cmd != '!')
                    Putc("+-*/%&|^"[cmd - 0x10]);
                Puts(": ");
                Expr(false);
                Putc('!');
                return true;
            }

            case '{': // Branch
                Conditional(branchEnd);
                return true;

            case '@': // Label jump
            case '\\': // Label call
                LabelRef();
                Putc(':');
                return true;

            case '&': // Page jump
            case '%': // Page call / return
                PageName(cmd);
                Putc(':');
                return true;

            case '<': // For-loop
                ForLoop();
                return true;

            case '>': // Loop end
                LoopEnd(branchEnd);
                return true;

            case ']': // Menu
                return true;

            case '$': // Menu item
                inMenuItem = !inMenuItem;
                if (inMenuItem)
                {
                    LabelRef();
                    Putc('$');
                    if (InlineMenuString())
                        inMenuItem = false;
                }
                return true;

            case '#': // Data table address
                DataTableAddr();
                return true;

            case '~': // Function call
                throw At(_p - 1, "function calls are not supported");

            case 'A':
            case 'R':
                return true;

            case 'B':
                return SubcommandNum() switch
                {
                    0 => Args("e"),
                    1 or 2 or 3 or 4 => Args("eeeeee"),
                    10 or 11 => Args("vv"),
                    12 or 13 or 14 => Args("v"),
                    21 or 22 or 23 or 24 or 31 or 32 or 33 or 34 => Args("evv"),
                    _ => false,
                };

            case 'G':
                return Current.Data[_p++] switch
                {
                    0 => Args("e"),
                    1 => Args("ee"),
                    _ => false,
                };

            case 'J':
                return SubcommandNum() switch
                {
                    0 or 1 or 2 or 3 => Args("ee"),
                    4 => Args(""),
                    _ => false,
                };

            case var _ when cmd == Cmd2('P', 'F') || cmd == Cmd2('P', 'W'):
                return SubcommandNum() switch
                {
                    0 or 1 => Args("e"),
                    2 or 3 => Args("ee"),
                    _ => false,
                };

            case var _ when cmd == Cmd2('P', 'T'):
                Putc(' ');
                return SubcommandNum() switch
                {
                    0 => Args("vee"),
                    1 => Args("vvvee"),
                    2 => Args("vvee"),
                    _ => false,
                };

            case var _ when cmd == Cmd2('S', 'G'):
                Putc(' ');
                return SubcommandNum() switch
                {
                    0 or 1 or 2 or 3 or 4 => Args("e"),
                    5 or 6 or 7 or 8 => Args("ee"),
                    _ => false,
                };

            case var _ when cmd == Cmd2('S', 'R'):
                // The new form; the old one ("ev") only sets xsys35c.cfg's old_SR besides.
                return Args(Current.Data[_p] < 0x40 ? "Nv" : "ev");

            case var _ when cmd == Cmd2('S', 'X'):
                Putc(' ');
                SubcommandNum(); // device
                Puts(", ");
                switch (SubcommandNum())
                {
                    case 1:
                        Puts(", ");
                        return Args("eee");
                    case 2:
                    case 4:
                        Puts(", ");
                        return Args("v");
                    case 3:
                        return true;
                    default:
                        return false;
                }

            case var _ when cmd == Cmd2('U', 'P'):
                Putc(' ');
                return SubcommandNum() switch
                {
                    0 => Args("ee"),
                    1 => Args("se"),
                    2 or 3 => Args("ss"),
                    _ => false,
                };

            case var _ when cmd == Cmd2('Z', 'T'):
                return SubcommandNum() switch
                {
                    2 or 3 or 4 or 5 => Args("v"),
                    0 or 1 or 20 or 21 => Args("e"),
                    10 => Args("eee"),
                    11 => Args("ev"),
                    _ => false,
                };

            case var _ when cmd == Cmd2F(0x21): // msg
                Putc('\'');
                _p = PutString(Current.Data, _p, 0, StringEscape);
                Putc('\'');
                return true;

            case var _ when cmd == Cmd2F(0x2b): // newUP
                Putc(' ');
                return SubcommandNum() switch
                {
                    0 => Args("ee"),
                    1 => Args("ze"),
                    2 or 3 => Args("zz"),
                    _ => false,
                };

            case var _ when cmd == Cmd2F(0x47): // menu
                return true;

            case var _ when cmd == Cmd2F(0x60): // dllCall: the .ain declares no DLL in this game
                throw At(_p - 2, $"DLL id out of range (dll:{Le32(_p)}, func:{Le32(_p + 4)})");

            case var _ when cmd == Cmd2F(0x7c):
                AinMsg(null, null);
                return true;
            case var _ when cmd == Cmd2F(0x7d):
                AinMsg("H", "ne");
                return true;
            case var _ when cmd == Cmd2F(0x7e):
                AinMsg("HH", "ne");
                return true;
            case var _ when cmd == Cmd2F(0x7f):
                AinMsg("X", "e");
                return true;

            default:
                return SignatureOf.TryGetValue(cmd, out var signature) && Args(signature);
        }
    }

    private bool Args(string sig)
    {
        Arguments(sig);
        return true;
    }
}
