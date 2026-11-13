using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace CSharpCode;

public sealed class PublicApiExtractor
{
    private StreamWriter? _writer;

    public static int ExtractFromPath(string rootPath, string? markerFileName, string outputPath, Action<string>? onMessageOutput)
    {
        // 输出根目录（相对于程序运行目录）
        string outputRoot = outputPath;

        // 开始前清空输出目录的所有内容（包括文件和子文件夹），但保留输出根目录本身
        if (Directory.Exists(outputRoot))
        {
            onMessageOutput?.Invoke($"正在清空输出目录: {outputRoot}");
            foreach (var dir in Directory.GetDirectories(outputRoot))
            {
                try
                {
                    Directory.Delete(dir, recursive: true);
                }
                catch (Exception ex)
                {
                    onMessageOutput?.Invoke($"清空目录失败 {dir}: {ex.Message}");
                }
            }

            foreach (var f in Directory.GetFiles(outputRoot))
            {
                try
                {
                    File.Delete(f);
                }
                catch (Exception ex)
                {
                    onMessageOutput?.Invoke($"删除文件失败 {f}: {ex.Message}");
                }
            }
        }

        onMessageOutput?.Invoke($"正在遍历路径: {rootPath}\r\n");
        var files = GetCsFiles(rootPath, markerFileName, onMessageOutput);
        var extractor = new PublicApiExtractor();

        foreach (var file in files)
        {
            try
            {
                string content = File.ReadAllText(file, Encoding.UTF8);

                // 输入文件相对于 rootPath 的相对路径
                string relativePath = Path.GetRelativePath(rootPath, file);

                // 让输出文件相对于 outputRoot 的路径与输入文件相对于 rootPath 的路径一致
                string outputFile = Path.Combine(outputRoot, relativePath) + ".txt";

                string? directory = Path.GetDirectoryName(outputFile);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                extractor.ExtractPublicDeclarations(content, outputFile);
            }
            catch (UnauthorizedAccessException ex)
            {
                onMessageOutput?.Invoke($"无权操作 {file}: {ex.Message}");
            }
            catch (IOException ex)
            {
                onMessageOutput?.Invoke($"操作文件失败 {file}: {ex.Message}");
            }
            catch (Exception ex)
            {
                onMessageOutput?.Invoke($"操作失败 {file}: {ex.Message}");
            }
        }

        onMessageOutput?.Invoke("\r\n操作完成");
        return files.Count;
    }

    /// <summary>
    /// 提取C#代码中的所有公共类型和公共成员声明，返回格式化的字符串
    /// </summary>
    /// <param name="code">要分析的C#源代码</param>
    public void ExtractPublicDeclarations(string code, string outputFile)
    {
        if (string.IsNullOrWhiteSpace(code))
            return;

        var encoding = new UTF8Encoding(true);
        var stream = new FileStream(outputFile,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read);
        _writer = new StreamWriter(stream, encoding);

        // 将源代码解析为语法树
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();

        // 获取 using 指令
        if (root is CompilationUnitSyntax compilationUnit)
        {
            if (compilationUnit.Usings.Count > 0)
            {
                _writer.WriteLine(compilationUnit.Usings);
                _writer.WriteLine();
            }
        }

        // 获取顶级成员
        var topLevelMembers = root.ChildNodes()
            .OfType<MemberDeclarationSyntax>()
            .Where(t => t.Parent is CompilationUnitSyntax);

        TraverseMember(topLevelMembers);
        _writer.Dispose();
    }

    /// <summary>
    /// 遍历目录及其子目录中的所有 .cs 文件，但忽略指定的文件夹。
    /// </summary>
    /// <param name="rootPath">要检查的根目录路径</param>
    /// <param name="markerFileName">可选：若指定，则仅当该文件存在于根目录时才执行遍历</param>
    /// <returns>所有 .cs 文件的完整路径列表</returns>
    private static List<string> GetCsFiles(string rootPath, string? markerFileName, Action<string>? onMessageOutput)
    {
        var csFiles = new List<string>();

        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
        {
            onMessageOutput?.Invoke($"目录不存在或无效: {rootPath}");
            return csFiles;
        }

        // 如果指定了标记文件，检查其是否存在
        if (!string.IsNullOrEmpty(markerFileName))
        {
            string markerPath = Path.Combine(rootPath, markerFileName);
            if (!File.Exists(markerPath))
            {
                onMessageOutput?.Invoke($"标记文件不存在，跳过遍历: {markerPath}");
                return csFiles;
            }
        }

        // 要忽略的目录名称（不区分大小写）
        var ignoredDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".git", ".vs", "bin", "obj"
        };

        // 使用栈进行深度优先遍历，避免递归栈溢出
        var stack = new Stack<string>();
        stack.Push(rootPath);

        while (stack.Count > 0)
        {
            string currentDir = stack.Pop();

            try
            {
                // 处理当前目录下的 .cs 文件
                foreach (string file in Directory.EnumerateFiles(currentDir, "*.cs", SearchOption.TopDirectoryOnly))
                {
                    csFiles.Add(file);
                    // 此处可以添加自定义处理逻辑，例如打印或分析文件
                    onMessageOutput?.Invoke($"找到 .cs 文件: {file}");
                }

                // 遍历子目录，跳过忽略的文件夹
                foreach (string subDir in Directory.EnumerateDirectories(currentDir, "*", SearchOption.TopDirectoryOnly))
                {
                    string dirName = Path.GetFileName(subDir);
                    if (!ignoredDirectories.Contains(dirName))
                    {
                        stack.Push(subDir);
                    }
                    else
                    {
                        onMessageOutput?.Invoke($"跳过忽略目录: {subDir}");
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                onMessageOutput?.Invoke($"无权访问目录: {currentDir}, 错误: {ex.Message}");
            }
            catch (IOException ex)
            {
                onMessageOutput?.Invoke($"I/O 错误访问目录: {currentDir}, 错误: {ex.Message}");
            }
        }

        return csFiles;
    }

    private void TraverseMember(IEnumerable<MemberDeclarationSyntax> nestedMembers, int indent = 0)
    {
        foreach (var nested in nestedMembers)
        {
            TraverseMember(nested, indent);
        }
    }

    private void TraverseMember(MemberDeclarationSyntax member, int indent = 0)
    {
        if (!IsPublic(member) && !IsExplicitInterfaceImplementation(member))
            return;

        var level = new string(' ', indent * 4);
        _writer!.Write(level);
        switch (member)
        {
            default:
                break;
            case EventFieldDeclarationSyntax evtField:
                // 简单事件
                FormatEventFieldDeclaration(evtField);
                break;
            case FieldDeclarationSyntax field:
                // 字段
                FormatFieldDeclaration(field);
                break;
            case ConstructorDeclarationSyntax ctor:
                // 构造函数
                FormatConstructorDeclaration(ctor);
                break;
            case DestructorDeclarationSyntax:
                // 析构函数
                break;
            case ConversionOperatorDeclarationSyntax conv:
                // 类型转换
                FormatConversionOperatorDeclaration(conv);
                break;
            case MethodDeclarationSyntax method:
                // 方法
                FormatMethodDeclaration(method);
                break;
            case OperatorDeclarationSyntax op:
                // 操作符
                FormatOperatorDeclaration(op);
                break;
            case FileScopedNamespaceDeclarationSyntax fsns:
                // 文件范围命名空间
                FormatFileScopedNamespaceDeclaration(fsns);
                _writer.WriteLine();
                if (fsns.Usings.Count > 0)
                {
                    _writer.WriteLine(fsns.Usings);
                    _writer.WriteLine();
                }
                var fsnsMembers = fsns.Members.OfType<MemberDeclarationSyntax>();
                TraverseMember(fsnsMembers, indent);
                break;
            case NamespaceDeclarationSyntax ns:
                // 块命名空间
                FormatNamespaceDeclaration(ns);
                _writer.Write(level);
                _writer.WriteLine('{');
                if (ns.Usings.Count > 0)
                {
                    _writer.WriteLine(ns.Usings);
                    _writer.WriteLine();
                }
                var nsMembers = ns.Members.OfType<MemberDeclarationSyntax>();
                TraverseMember(nsMembers, indent + 1);
                _writer.Write(level);
                _writer.Write('}');
                break;
            case EventDeclarationSyntax evt:
                // 完整事件
                FormatEventDeclaration(evt);
                break;
            case IndexerDeclarationSyntax indexer:
                // 索引器
                FormatIndexerDeclaration(indexer);
                break;
            case PropertyDeclarationSyntax property:
                // 属性
                FormatPropertyDeclaration(property);
                break;
            case EnumDeclarationSyntax @enum:
                // 枚举
                FormatEnumDeclaration(@enum);
                _writer.Write(level);
                _writer.WriteLine('{');
                var enumMembers = @enum.Members.OfType<MemberDeclarationSyntax>();
                TraverseMember(enumMembers, indent + 1);
                _writer.Write(level);
                _writer.Write('}');
                break;
            case TypeDeclarationSyntax type:
                // 类
                FormatTypeDeclaration(type);
                _writer.Write(level);
                _writer.WriteLine('{');
                var typeMembers = type.Members.OfType<MemberDeclarationSyntax>();
                TraverseMember(typeMembers, indent + 1);
                _writer.Write(level);
                _writer.Write('}');
                break;
            case DelegateDeclarationSyntax @delegate:
                // 委托
                FormatDelegateDeclaration(@delegate);
                break;
            case EnumMemberDeclarationSyntax enumMember:
                // 枚举成员
                FormatEnumMemberDeclaration(enumMember);
                break;
            case GlobalStatementSyntax:
                // 顶级语句
                break;
            case IncompleteMemberSyntax:
                break;
        }
        _writer.WriteLine();
    }

    private static bool IsPublic(MemberDeclarationSyntax member)
    {
        if (member is FileScopedNamespaceDeclarationSyntax or NamespaceDeclarationSyntax
            or EnumMemberDeclarationSyntax or GlobalStatementSyntax)
        {
            return true;
        }
        else
        {
            foreach (var modifier in member.Modifiers)
            {
                if (modifier.IsKind(SyntaxKind.PublicKeyword))
                {
                    return true;
                }
                else if (modifier.IsKind(SyntaxKind.PrivateKeyword)
                    || modifier.IsKind(SyntaxKind.InternalKeyword)
                    || modifier.IsKind(SyntaxKind.ProtectedKeyword))
                {
                    return false;
                }
            }
            // 没有访问修饰符
            return member.Parent is InterfaceDeclarationSyntax || member is ExtensionBlockDeclarationSyntax;
        }
    }

    private static bool IsExplicitInterfaceImplementation(MemberDeclarationSyntax member)
    {
        var explicitInterfaceSpecifier = member switch
        {
            ConversionOperatorDeclarationSyntax conv => conv.ExplicitInterfaceSpecifier, // 类型转换
            MethodDeclarationSyntax method => method.ExplicitInterfaceSpecifier, // 方法
            OperatorDeclarationSyntax op => op.ExplicitInterfaceSpecifier, // 操作符
            EventDeclarationSyntax evt => evt.ExplicitInterfaceSpecifier, // 完整事件
            IndexerDeclarationSyntax indexer => indexer.ExplicitInterfaceSpecifier, // 索引器
            PropertyDeclarationSyntax property => property.ExplicitInterfaceSpecifier, // 属性
            _ => null,
        };
        return explicitInterfaceSpecifier != null;
    }

    private void FormatEventFieldDeclaration(EventFieldDeclarationSyntax eventField)
    {
        if (eventField.Modifiers.Count > 0)
        {
            _writer!.Write(eventField.Modifiers);
            _writer.Write(' ');
        }
        _writer!.Write("event ");
        _writer.Write(eventField.Declaration.Type);
        _writer.Write(' ');
        _writer.Write(eventField.Declaration.Variables[0].Identifier.Text);
        for (int i = 1; i < eventField.Declaration.Variables.Count; i++)
        {
            var variable = eventField.Declaration.Variables[i];
            _writer.Write(", ");
            _writer.Write(variable.Identifier.Text);
        }
        _writer.Write(';');
    }

    private void FormatFieldDeclaration(FieldDeclarationSyntax field)
    {
        if (field.Modifiers.Count > 0)
        {
            _writer!.Write(field.Modifiers);
            _writer.Write(' ');
        }
        _writer!.Write(field.Declaration.Type);
        _writer.Write(' ');
        _writer.Write(field.Declaration.Variables[0].Identifier.Text);
        for (int i = 1; i < field.Declaration.Variables.Count; i++)
        {
            var variable = field.Declaration.Variables[i];
            _writer.Write(", ");
            _writer.Write(variable.Identifier.Text);
        }
        _writer.Write(';');
    }

    private void FormatConstructorDeclaration(ConstructorDeclarationSyntax ctor)
    {
        if (ctor.Modifiers.Count > 0)
        {
            _writer!.Write(ctor.Modifiers);
            _writer.Write(' ');
        }
        _writer!.Write(ctor.Identifier.Text);
        _writer.Write(ctor.ParameterList);
        _writer.Write(ctor.Initializer);
        _writer.Write(';');
    }

    private void FormatConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax conv)
    {
        if (conv.Modifiers.Count > 0)
        {
            _writer!.Write(conv.Modifiers);
            _writer.Write(' ');
        }
        _writer!.Write(conv.ImplicitOrExplicitKeyword.Text);
        _writer.Write(' ');
        _writer.Write(conv.ExplicitInterfaceSpecifier);
        _writer.Write("operator ");
        _writer.Write(conv.Type);
        _writer.Write(conv.ParameterList);
        _writer.Write(';');
    }

    private void FormatMethodDeclaration(MethodDeclarationSyntax method)
    {
        if (method.Modifiers.Count > 0)
        {
            _writer!.Write(method.Modifiers);
            _writer.Write(' ');
        }
        _writer!.Write(method.ReturnType);
        _writer.Write(' ');
        _writer.Write(method.ExplicitInterfaceSpecifier);
        _writer.Write(method.Identifier.Text);
        _writer.Write(method.TypeParameterList);
        _writer.Write(method.ParameterList);
        if (method.ConstraintClauses.Count > 0)
        {
            _writer.Write(' ');
            _writer.Write(method.ConstraintClauses);
        }
        _writer.Write(';');
    }

    private void FormatOperatorDeclaration(OperatorDeclarationSyntax op)
    {
        if (op.Modifiers.Count > 0)
        {
            _writer!.Write(op.Modifiers);
            _writer.Write(' ');
        }
        _writer!.Write(op.ReturnType);
        _writer.Write(' ');
        _writer.Write(op.ExplicitInterfaceSpecifier);
        _writer.Write("operator ");
        _writer.Write(op.OperatorToken.Text);
        _writer.Write(op.ParameterList);
        _writer.Write(';');
    }

    private void FormatFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax fsns)
    {
        _writer!.Write("namespace ");
        _writer.Write(fsns.Name);
        _writer.WriteLine(';');
    }

    private void FormatNamespaceDeclaration(NamespaceDeclarationSyntax ns)
    {
        _writer!.Write("namespace ");
        _writer.WriteLine(ns.Name);
    }

    private void FormatEventDeclaration(EventDeclarationSyntax @event)
    {
        if (@event.Modifiers.Count > 0)
        {
            _writer!.Write(@event.Modifiers);
            _writer.Write(' ');
        }
        _writer!.Write("event ");
        _writer.Write(@event.Type);
        _writer.Write(' ');
        _writer.Write(@event.ExplicitInterfaceSpecifier);
        _writer.Write(@event.Identifier.Text);
        _writer.Write(' ');
        FormatAccessors(@event);
    }

    private void FormatIndexerDeclaration(IndexerDeclarationSyntax indexer)
    {
        if (indexer.Modifiers.Count > 0)
        {
            _writer!.Write(indexer.Modifiers);
            _writer.Write(' ');
        }
        _writer!.Write(indexer.Type);
        _writer.Write(' ');
        _writer.Write(indexer.ExplicitInterfaceSpecifier);
        _writer.Write("this");
        _writer.Write(indexer.ParameterList);
        _writer.Write(' ');
        FormatAccessors(indexer);
    }

    private void FormatPropertyDeclaration(PropertyDeclarationSyntax property)
    {
        if (property.Modifiers.Count > 0)
        {
            _writer!.Write(property.Modifiers);
            _writer.Write(' ');
        }
        _writer!.Write(property.Type);
        _writer.Write(' ');
        _writer.Write(property.ExplicitInterfaceSpecifier);
        _writer.Write(property.Identifier.Text);
        FormatAccessors(property);
    }

    private void FormatEnumDeclaration(EnumDeclarationSyntax @enum)
    {
        if (@enum.Modifiers.Count > 0)
        {
            _writer!.Write(@enum.Modifiers);
            _writer.Write(' ');
        }
        _writer!.Write("enum ");
        _writer.Write(@enum.Identifier.Text);
        _writer.Write(' ');
        _writer.WriteLine(@enum.BaseList);
    }

    private void FormatTypeDeclaration(TypeDeclarationSyntax type)
    {
        if (type.Modifiers.Count > 0)
        {
            _writer!.Write(type.Modifiers);
            _writer.Write(' ');
        }
        var keyword = type.Keyword.Text;
        _writer!.Write(keyword);
        if (keyword != "extension")
            _writer.Write(' ');
        if (type is RecordDeclarationSyntax record)
        {
            _writer.Write(record.ClassOrStructKeyword);
            _writer.Write(' ');
        }
        _writer.Write(type.Identifier.Text);
        _writer.Write(type.TypeParameterList);
        _writer.Write(type.ParameterList);
        _writer.Write(' ');
        _writer.Write(type.BaseList);
        _writer.Write(' ');
        _writer.WriteLine(type.ConstraintClauses);
    }

    private void FormatDelegateDeclaration(DelegateDeclarationSyntax @delegate)
    {
        if (@delegate.Modifiers.Count > 0)
        {
            _writer!.Write(@delegate.Modifiers);
            _writer.Write(' ');
        }
        _writer!.Write("delegate ");
        _writer.Write(@delegate.ReturnType);
        _writer.Write(' ');
        _writer.Write(@delegate.Identifier.Text);
        _writer.Write(@delegate.TypeParameterList);
        _writer.Write(@delegate.ParameterList);
        _writer.Write(' ');
        _writer.Write(@delegate.ConstraintClauses);
        _writer.Write(';');
    }

    private void FormatEnumMemberDeclaration(EnumMemberDeclarationSyntax enumMember)
    {
        _writer!.Write(enumMember.Identifier.Text);
        if (enumMember.EqualsValue is not null)
        {
            _writer.Write(' ');
            _writer.Write(enumMember.EqualsValue);
        }
        _writer.Write(',');
    }

    private void FormatAccessors(BasePropertyDeclarationSyntax baseProperty)
    {
        _writer!.Write(" { ");

        // 如果属性有访问器列表（通常的 get/set/init）
        if (baseProperty.AccessorList != null)
        {
            foreach (var accessor in baseProperty.AccessorList.Accessors)
            {
                // 检查访问器是否为 public
                bool isPublic = true;

                // 遍历访问器的修饰符
                foreach (var modifier in accessor.Modifiers)
                {
                    // 如果出现 private/internal/protected，则该访问器不是 public
                    if (modifier.IsKind(SyntaxKind.PrivateKeyword) ||
                        modifier.IsKind(SyntaxKind.InternalKeyword) ||
                        modifier.IsKind(SyntaxKind.ProtectedKeyword))
                    {
                        isPublic = false;
                        break;
                    }
                    // 显式的 public 修饰符不影响 isPublic（默认为 true）
                }

                // 没有显式非 public 修饰符 → 继承属性的 public 性 → 视为 public
                if (isPublic)
                {
                    _writer.Write(accessor.Keyword.Text); // "get", "set", "init"
                    _writer.Write("; ");
                }
            }
        }
        else if ((baseProperty is PropertyDeclarationSyntax property && property.ExpressionBody != null)
            || (baseProperty is IndexerDeclarationSyntax indexer && indexer.ExpressionBody != null))
        {
            // 表达式体属性（如 public int P => 42;）→ 只有 getter，视为 public
            _writer.Write("get; ");
        }

        _writer.Write("}");
    }
}
