# Expected behavior

The completed workspace should contain a root-level Blazor Server chat application with a ChatGPT-style single-page UI.

Expected user behavior:

- The page renders a sidebar for conversations and a main chat area.
- A user can start a new chat.
- A user can type a prompt and send it.
- The sent prompt appears as a user message.
- A deterministic local assistant service adds an assistant response without using external APIs.
- Empty prompts are not sent.
- Components expose stable `data-testid` selectors for automated validation.

Expected engineering behavior:

- Tailwind CSS is configured and used for styling.
- The main web project and solution file are at the workspace root.
- The design follows MVVM: multiple focused ViewModels own UI state/actions; Razor components focus on rendering and binding.
- Services depend on abstractions and are registered through dependency injection.
- Required UI components have corresponding automated component tests.
- `README.md`, `Dockerfile`, and `docker-compose.yml` or `compose.yml` are present and meaningful.
- No real secrets, credentials, API keys, or tokens are committed.
- CodePass analysis with `.codepass/rules` passes without rule errors.
- `dotnet list package --vulnerable --include-transitive` reports no vulnerable package versions.
