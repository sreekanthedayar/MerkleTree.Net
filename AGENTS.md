# AI Agent Guidelines

This file is the working contract for AI agents contributing to this repository.

## Repository scope

- `MerkleTree/` contains the .NET Merkle-tree library.
- `MerkleTree.Tests/` contains the automated tests.
- `README.md` documents the public API and usage examples.

## General rules

1. Read this file before making repository changes.
2. Inspect only the files relevant to the requested task.
3. Preserve existing user changes and unrelated work.
4. Do not make assumptions that would materially change public APIs, security behavior, compatibility, or data formats; call them out before proceeding.
5. Keep changes focused. Avoid unrelated refactoring, speculative abstractions, and formatting-only edits.
6. Follow the existing C# style, nullable-reference conventions, and project structure.
7. Use `rg` or `rg --files` for repository searches.
8. Use `apply_patch` for source and documentation edits.
9. Do not commit, push, or perform destructive Git operations unless explicitly requested.

## Merkle-tree and cryptographic behavior

- Treat hash construction, proof formats, tree shape, and serialized metadata as compatibility-sensitive.
- Do not change the hash algorithm or hash encoding as part of an unrelated fix.
- Security-sensitive verification must validate the complete commitment it claims to verify, not merely reproduce one supplied value.
- Prefer bounded proof verification over rebuilding a complete tree when the proof format permits it.
- Add focused negative tests for malformed, incomplete, reordered, or mismatched proofs when changing verification logic.
- Preserve existing behavior for supported hash algorithms unless the task explicitly changes that contract.

## Tests and verification

- For a bug fix, first add a regression test that asserts the intended behavior and fails against the current implementation.
- Apply the production fix, then confirm that the original regression test passes without weakening it.
- Run focused tests for the changed behavior, followed by the full test suite when practical.
- Use the normal project `bin/` and `obj/` build outputs; do not introduce alternate build directories.
- Do not claim a test or verification passed unless it was actually run.
- Do not remove or weaken tests merely to make the suite pass.

Typical commands:

```powershell
dotnet test --no-restore --filter FullyQualifiedName~TestName
dotnet test --no-restore
```

## Public API and documentation

- Consider source and serialized proof APIs public contracts.
- If a public API, proof format, or usage behavior changes, update the relevant tests and `README.md`.
- Prefer additive, clearly named API changes when compatibility matters; obsolete insecure overloads rather than silently treating them as secure.

## Completion report

Report only verified facts:

- what changed;
- affected files;
- tests run and their results;
- remaining open items or compatibility concerns.
- Include a suggested commit message in Conventional Commits format (`type(scope): description`); do not create the commit unless explicitly requested.
