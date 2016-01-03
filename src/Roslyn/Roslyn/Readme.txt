MSBuilder: Roslyn
=========================================

Allows custom tasks to efficiently load and access
a Roslyn project for code generation or analysis. Multiple tasks, targets
and even projects will all share the same Workspace and loaded Projects,
thereby optimizing build times.

Usage:
In your custom task library, install the MSBuilder.Roslyn package, and just
use the two new provided extension methods for Task: GetWorkspace and GetOrAddProject.

