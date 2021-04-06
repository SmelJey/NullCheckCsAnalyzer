# NullCheckCsAnalyzer
Null reference diagnostic C# compiler extension

Checks if null check is not needed and provides code-fixes in next situations:
+ Null check with equality operators (==, !=)
+ Conditional operator (?:)
+ Null propagation (?.)
+ Null coalescing (??, ??=)
+ .Equals and ReferenceEquals methods
