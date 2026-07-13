using Microsoft.SqlServer.Dac.Model;

namespace DacPac.Core;

public static class ExtensionMethods
{

    /// <summary>
    /// Returns the equivalent .NET data type for a given SQL data type name. If the SQL data type is not recognized, it returns null.
    /// </summary>
    /// <param name="sqlDataTypeName"></param>
    /// <param name="nullable"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static DotnetType? GetDotNetDataType(string sqlDataTypeName, bool nullable = false)
    {
        if (sqlDataTypeName == null) throw new ArgumentNullException(nameof(sqlDataTypeName));
        var result = sqlDataTypeName.ToLower() switch
        {
            "bigint" => new DotnetType("long", nullable),
            "binary" or "image" or "varbinary" => new DotnetType("byte[]", false),
            "bit" => new DotnetType("bool", nullable),
            "char" => new DotnetType("char", nullable),
            "datetime" or "smalldatetime" => new DotnetType("System.DateTime", nullable),
            "decimal" or "money" or "numeric" => new DotnetType("decimal", nullable),
            "float" => new DotnetType("double", nullable),
            "int" => new DotnetType("int", nullable),
            "nchar" or "nvarchar" or "text" or "varchar" or "xml" => new DotnetType("string", false),
            "real" => new DotnetType("float", nullable),
            "smallint" => new DotnetType("short", nullable),
            "tinyint" => new DotnetType("byte", nullable),
            "uniqueidentifier" => new DotnetType("System.Guid", nullable),
            "date" => new DotnetType("System.DateTime", nullable),
            _ => null
        };
        if (result != null)
        {
            return result;
        }
        
        return null;
    }
    
    
    /// <summary>
    /// Returns the equivalent .NET data type for a given SQL data type name. If the SQL data type is not recognized, it returns null.
    /// </summary>
    /// <param name="sqlDataTypeName"></param>
    /// <param name="nullable"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static DotnetType? GetDotNetDataType(this SqlDataType sqlDataTypeName, bool nullable = false)
    {
        switch (sqlDataTypeName)
        {
            // Integer types
            case SqlDataType.BigInt: return new DotnetType("long", nullable);
            case SqlDataType.Int: return new DotnetType("int", nullable);
            case SqlDataType.SmallInt: return new DotnetType("short", nullable);
            case SqlDataType.TinyInt: return new DotnetType("byte", nullable);
            case SqlDataType.Bit: return new DotnetType("bool", nullable);

            // Exact / approximate numeric types
            case SqlDataType.Decimal:
            case SqlDataType.Numeric:
            case SqlDataType.Money:
            case SqlDataType.SmallMoney: return new DotnetType("decimal", nullable);
            case SqlDataType.Float: return new DotnetType("double", nullable);
            case SqlDataType.Real: return new DotnetType("float", nullable);

            // Date & time types
            case SqlDataType.DateTime:
            case SqlDataType.SmallDateTime:
            case SqlDataType.DateTime2:
            case SqlDataType.Date: return new DotnetType("System.DateTime", nullable);
            case SqlDataType.Time: return new DotnetType("System.TimeSpan", nullable);
            case SqlDataType.DateTimeOffset: return new DotnetType("System.DateTimeOffset", nullable);

            // Character / text types (reference types are never suffixed with '?')
            case SqlDataType.Char: return new DotnetType("char", nullable);
            case SqlDataType.VarChar:
            case SqlDataType.Text:
            case SqlDataType.NChar:
            case SqlDataType.NVarChar:
            case SqlDataType.NText:
            case SqlDataType.Xml:
            case SqlDataType.Json: return new DotnetType("string", false);

            // Binary types
            case SqlDataType.Binary:
            case SqlDataType.VarBinary:
            case SqlDataType.Image:
            case SqlDataType.Timestamp:
            case SqlDataType.Rowversion: return new DotnetType("byte[]", nullable);

            // Other scalar types
            case SqlDataType.UniqueIdentifier: return new DotnetType("System.Guid", nullable);
            case SqlDataType.Variant: return new DotnetType("object", nullable);

            // Types that have no meaningful CLR representation as a column value
            case SqlDataType.Unknown:
            case SqlDataType.Cursor:
            case SqlDataType.Table:
            case SqlDataType.Vector: return null;

            default:
                throw new ArgumentOutOfRangeException(nameof(sqlDataTypeName), sqlDataTypeName, null);
        }
    }
    
    public static DotnetType? GetDotNetDataType(this TSqlObject sqlDataTypeName, bool nullable = false)
    {
        if (sqlDataTypeName.ObjectType != DataType.TypeClass)
        {
            return null;
        }

        if (sqlDataTypeName.GetReferenced(DataType.Type).FirstOrDefault() is { } underlying)
        {
            return GetDotNetDataType(underlying.GetProperty<SqlDataType>(DataType.SqlDataType), nullable);
        }
        
        var sqlDataType = sqlDataTypeName.GetProperty<SqlDataType>(DataType.SqlDataType);
        
        switch (sqlDataType)
        {
            // Integer types
            case SqlDataType.BigInt: return new DotnetType("long", nullable);
            case SqlDataType.Int: return new DotnetType("int", nullable);
            case SqlDataType.SmallInt: return new DotnetType("short", nullable);
            case SqlDataType.TinyInt: return new DotnetType("byte", nullable);
            case SqlDataType.Bit: return new DotnetType("bool", nullable);

            // Exact / approximate numeric types
            case SqlDataType.Decimal:
            case SqlDataType.Numeric:
            case SqlDataType.Money:
            case SqlDataType.SmallMoney: return new DotnetType("decimal", nullable);
            case SqlDataType.Float: return new DotnetType("double", nullable);
            case SqlDataType.Real: return new DotnetType("float", nullable);

            // Date & time types
            case SqlDataType.DateTime:
            case SqlDataType.SmallDateTime:
            case SqlDataType.DateTime2:
            case SqlDataType.Date: return new DotnetType("System.DateTime", nullable);
            case SqlDataType.Time: return new DotnetType("System.TimeSpan", nullable);
            case SqlDataType.DateTimeOffset: return new DotnetType("System.DateTimeOffset", nullable);

            // Character / text types (reference types are never suffixed with '?')
            case SqlDataType.Char: return new DotnetType("char", nullable);
            case SqlDataType.VarChar:
            case SqlDataType.Text:
            case SqlDataType.NChar:
            case SqlDataType.NVarChar:
            case SqlDataType.NText:
            case SqlDataType.Xml:
            case SqlDataType.Json: return new DotnetType("string", false);

            // Binary types
            case SqlDataType.Binary:
            case SqlDataType.VarBinary:
            case SqlDataType.Image:
            case SqlDataType.Timestamp:
            case SqlDataType.Rowversion: return new DotnetType("byte[]", nullable);

            // Other scalar types
            case SqlDataType.UniqueIdentifier: return new DotnetType("System.Guid", nullable);
            case SqlDataType.Variant: return new DotnetType("object", nullable);

            // Types that have no meaningful CLR representation as a column value
            case SqlDataType.Unknown:
            case SqlDataType.Cursor:
            case SqlDataType.Table:
            case SqlDataType.Vector: return null;

            default:
                throw new ArgumentOutOfRangeException(nameof(sqlDataTypeName), sqlDataTypeName, null);
        }
    }
    
    /// <summary>
    /// Converts a raw database identifier (such as a table or column name) into a
    /// <b>PascalCase</b> identifier suitable for a C# type or property name.
    /// </summary>
    /// <remarks>
    /// PascalCase means every word starts with an upper-case letter and there are no
    /// separators between words. The method treats <c>_</c>, <c>-</c> and spaces as word
    /// separators, removes them, and upper-cases the first letter of each remaining word.
    /// <para>
    /// Characters that are not the start of a word keep their original case, so an
    /// existing acronym like <c>ID</c> is preserved rather than turned into <c>Id</c>.
    /// If the database name happens to start with a digit (which is illegal for a C#
    /// identifier) a single underscore is prepended.
    /// </para>
    /// <example>
    /// <list type="bullet">
    /// <item><description><c>customer_id</c> → <c>CustomerId</c></description></item>
    /// <item><description><c>first name</c> → <c>FirstName</c></description></item>
    /// <item><description><c>FirstName</c> → <c>FirstName</c> (unchanged)</description></item>
    /// <item><description><c>2nd_address</c> → <c>_2ndAddress</c></description></item>
    /// </list>
    /// </example>
    /// </remarks>
    /// <param name="name">The raw database identifier to convert.</param>
    /// <returns>
    /// The PascalCase form of <paramref name="name"/>, or the original value when it is
    /// <see langword="null"/> or empty.
    /// </returns>
    public static string ToPascalCase(this string name) => ToCasedIdentifier(name, upperFirstWord: true);

    /// <summary>
    /// Converts a raw database identifier (such as a column name) into a valid C#
    /// <b>parameter name</b>: a <c>camelCase</c> identifier that is also safe to use even
    /// when it collides with a C# language keyword.
    /// </summary>
    /// <remarks>
    /// This produces the same word-splitting and casing as <see cref="ToPascalCase"/>, but
    /// the very first letter is <b>lower-cased</b> (camelCase) to match the usual C#
    /// convention for parameters and local variables.
    /// <para>
    /// If the resulting word is a reserved C# keyword (for example <c>class</c>, <c>int</c>
    /// or <c>params</c>) it is prefixed with <c>@</c> so the generated code still compiles.
    /// A leading digit is escaped with an underscore, exactly as in <see cref="ToPascalCase"/>.
    /// </para>
    /// <example>
    /// <list type="bullet">
    /// <item><description><c>customer_id</c> → <c>customerId</c></description></item>
    /// <item><description><c>FirstName</c> → <c>firstName</c></description></item>
    /// <item><description><c>class</c> → <c>@class</c></description></item>
    /// </list>
    /// </example>
    /// </remarks>
    /// <param name="name">The raw database identifier to convert.</param>
    /// <returns>
    /// A camelCase parameter name that is guaranteed to be a legal C# identifier, or the
    /// original value when <paramref name="name"/> is <see langword="null"/> or empty.
    /// </returns>
    public static string ToParameterName(this string name)
    {
        var camel = ToCasedIdentifier(name, upperFirstWord: false);
        return CSharpKeywords.Contains(camel) ? "@" + camel : camel;
    }

    /// <summary>
    /// Shared core for <see cref="ToPascalCase"/> and <see cref="ToParameterName"/>. Walks the
    /// input once, dropping separators (<c>_</c>, <c>-</c>, space) and casing the first letter of
    /// each word. The whole transformation happens in a single stack-allocated buffer for short
    /// names (no heap allocation other than the returned string), falling back to the heap only
    /// for unusually long identifiers.
    /// </summary>
    /// <param name="name">The raw identifier to convert.</param>
    /// <param name="upperFirstWord">
    /// <see langword="true"/> to upper-case the first word (PascalCase); <see langword="false"/>
    /// to lower-case it (camelCase). Subsequent words are always upper-cased.
    /// </param>
    private static string ToCasedIdentifier(string name, bool upperFirstWord)
    {
        if (string.IsNullOrEmpty(name)) return name;

        // The result is never longer than the input plus one extra slot reserved for a
        // possible leading underscore (when the name starts with a digit). We only ever
        // remove separators, so this single buffer is always large enough.
        Span<char> buffer = name.Length < 256 ? stackalloc char[name.Length + 1] : new char[name.Length + 1];
        var pos = 0;
        var startOfWord = true; // the next non-separator character begins a new word
        var firstWord = true;   // are we still building the very first word?

        foreach (var c in name)
        {
            if (c is '_' or '-' or ' ')
            {
                startOfWord = true;
                continue;
            }

            if (startOfWord)
            {
                if (pos == 0 && char.IsDigit(c))
                    buffer[pos++] = '_'; // C# identifiers may not start with a digit

                buffer[pos++] = firstWord && !upperFirstWord
                    ? char.ToLowerInvariant(c)
                    : char.ToUpperInvariant(c);

                startOfWord = false;
                firstWord = false;
            }
            else
            {
                buffer[pos++] = c;
            }
        }

        return new string(buffer[..pos]);
    }

    /// <summary>
    /// The full set of reserved C# keywords that cannot be used as a bare identifier and must
    /// therefore be escaped with a leading <c>@</c> when emitted as a parameter name.
    /// </summary>
    private static readonly HashSet<string> CSharpKeywords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
        "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
        "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
        "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
        "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
        "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
        "void", "volatile", "while"
    };
}