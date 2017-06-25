// dnlib: See LICENSE.txt for more info

using System.Diagnostics.SymbolStore;

namespace dnlib.DotNet.Pdb.Dss {
	sealed class SymbolScope : ISymbolScope {
		readonly ISymUnmanagedScope scope;

		public SymbolScope(ISymUnmanagedScope scope) {
			this.scope = scope;
		}

		public int EndOffset {
			get {
				scope.GetEndOffset(out uint result);
				return (int)result;
			}
		}

		public ISymbolMethod Method {
			get {
                scope.GetMethod(out ISymUnmanagedMethod method);
                return method == null ? null : new SymbolMethod(method);
			}
		}

		public ISymbolScope Parent {
			get {
                scope.GetParent(out ISymUnmanagedScope parentScope);
                return parentScope == null ? null : new SymbolScope(parentScope);
			}
		}

		public int StartOffset {
			get {
                scope.GetStartOffset(out uint result);
                return (int)result;
			}
		}

		public ISymbolScope[] GetChildren() {
            scope.GetChildren(0, out uint numScopes, null);
            var unScopes = new ISymUnmanagedScope[numScopes];
			scope.GetChildren((uint)unScopes.Length, out numScopes, unScopes);
			var scopes = new ISymbolScope[numScopes];
			for (uint i = 0; i < numScopes; i++)
				scopes[i] = new SymbolScope(unScopes[i]);
			return scopes;
		}

		public ISymbolVariable[] GetLocals() {
            scope.GetLocals(0, out uint numVars, null);
            var unVars = new ISymUnmanagedVariable[numVars];
			scope.GetLocals((uint)unVars.Length, out numVars, unVars);
			var vars = new ISymbolVariable[numVars];
			for (uint i = 0; i < numVars; i++)
				vars[i] = new SymbolVariable(unVars[i]);
			return vars;
		}

		public ISymbolNamespace[] GetNamespaces() {
            scope.GetNamespaces(0, out uint numNss, null);
            var unNss = new ISymUnmanagedNamespace[numNss];
			scope.GetNamespaces((uint)unNss.Length, out numNss, unNss);
			var nss = new ISymbolNamespace[numNss];
			for (uint i = 0; i < numNss; i++)
				nss[i] = new SymbolNamespace(unNss[i]);
			return nss;
		}
	}
}
