using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EclipseSourceConverter
{
    struct TypeMapping
    {
        public readonly SyntaxNode Type;
        public readonly bool IsContainer;

        public TypeMapping(SyntaxNode type, bool isContainer) {
            this.Type = type;
            this.IsContainer = isContainer;
        }
    }
}
