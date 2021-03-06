﻿using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SecurityCodeScan.Analyzers.Utils;

namespace SecurityCodeScan.Analyzers.Taint
{
    /// <summary>
    /// Execution state at a given statement. It will be alter after each statement are evaluated.
    /// 
    /// Objectives of this class:
    ///  * Keep the state of each variable
    ///  * Keep reference to utilities that are require along the taint analysis. (Such as resolving symbol)
    /// </summary>
    public class ExecutionState
    {
        private bool DebugMode = true;

        public SyntaxNodeAnalysisContext          AnalysisContext { get; }
        public IDictionary<string, VariableState> Variables       { get; }

        /// <summary>
        /// Initialize the state with no variable recorded yet.
        /// </summary>
        /// <param name="ctx">Context used to resolve symbol.</param>
        public ExecutionState(SyntaxNodeAnalysisContext ctx)
        {
            AnalysisContext = ctx;
            Variables       = new Dictionary<string, VariableState>();
        }

        public void AddNewValue(string identifier, VariableState value)
        {
            if (Variables.ContainsKey(identifier)) //New variable in a different scope
            {
                if (DebugMode)
                    Logger.Log("Removing existing state for " + identifier);

                Variables.Remove(identifier);
            }

            if (DebugMode)
                Logger.Log($"Adding state for {identifier} ({value})");

            Variables.Add(identifier, value);
        }

        public void MergeValue(string identifier, VariableState value)
        {
            if (Variables.ContainsKey(identifier)) //Override existing value
            {
                var state    = Variables[identifier];
                var newState = state.Merge(value);
                Variables.Remove(identifier);
                Variables.Add(identifier, newState);
                if (DebugMode)
                    Logger.Log($"Merging state for {identifier} ({newState})");
            }
            else
            {
                //Unexpected state
                if (DebugMode)
                    Logger.Log($"Merging state for {identifier} ({value}) .. /!\\ unexpected state");

                Variables.Add(identifier, value);
            }
        }

        public VariableState GetValueByIdentifier(string identifier)
        {
            return Variables.TryGetValue(identifier, out var value) ? value : new VariableState(value.Node, VariableTaint.Unknown);
        }

        /// <summary>
        /// Resolve semantic class name of a given variable reference.
        /// </summary>
        /// <param name="node">Expression to evaluate</param>
        /// <returns>The resolved symbol with the complete class name and method name.</returns>
        public ISymbol GetSymbol(SyntaxNode node)
        {
            return node != null ? AnalysisContext.SemanticModel.GetSymbolInfo(node).Symbol : null;
        }

        public void AddTag(string variableAccess, VariableTag httpCookieSecure)
        {
            if (DebugMode)
                Logger.Log($"Adding tag '{httpCookieSecure}' to  {variableAccess}");

            if (Variables.TryGetValue(variableAccess, out var variable))
                variable.AddTag(httpCookieSecure);
        }
    }
}
