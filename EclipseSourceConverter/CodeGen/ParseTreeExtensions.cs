using Antlr4.Runtime.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EclipseSourceConverter.CodeGen
{
    public static class ParseTreeExtensions
    {
        public static IEnumerable<IParseTree> EnumerateChildContexts(this IParseTree parseTree) {
            for(int i = 0; i < parseTree.ChildCount; i++) {
                yield return parseTree.GetChild(i);
            }
        }
    }
}
