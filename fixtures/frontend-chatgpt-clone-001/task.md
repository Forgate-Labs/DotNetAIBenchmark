# Frontend ChatGPT-style Blazor Server app

Create a new Blazor Server application from scratch in this workspace.

The application must be a single-page chat experience that imitates the high-level ChatGPT interface: a conversation sidebar, a main message area, a message composer, user/assistant message bubbles, and a new-chat flow.

## Required technology

- .NET 10.
- Blazor Server / interactive server rendering.
- Tailwind CSS for styling.
- MVVM architecture.
- SOLID design principles.

## Required workspace layout

- Put the web application project at the workspace root.
- Create a root-level `.sln` file that includes the web project and test project.
- Do not create a nested application folder such as `src/`, `app/`, `ChatApp/`, or another subdirectory that contains the main `.csproj`.
- Supporting folders such as `Components/`, `ViewModels/`, `Models/`, `Services/`, `wwwroot/`, and `tests/` are allowed.

## Required UI components

Create these components and keep them focused:

- `ChatPage.razor`
- `ChatShell.razor`
- `ConversationSidebar.razor`
- `MessageList.razor`
- `MessageBubble.razor`
- `MessageComposer.razor`

The rendered UI must include stable selectors for deterministic validation:

- `data-testid="chat-layout"`
- `data-testid="conversation-sidebar"`
- `data-testid="message-list"`
- `data-testid="message-composer"`
- `data-testid="prompt-input"`
- `data-testid="send-button"`
- `data-testid="new-chat-button"`
- `data-testid="user-message"`
- `data-testid="assistant-message"`

## Architecture requirements

- Use ViewModels for UI state and UI actions.
- Keep business/chat orchestration out of Razor components.
- Use abstractions for services, such as interfaces for chat response generation and conversation state/persistence.
- Register dependencies through DI.
- Use a deterministic local assistant response service. Do not call external LLM APIs.
- Do not store secrets or credentials in any file.
- Use the CodePass CLI with the rules under `.codepass/rules` before finishing:
  `codepass analyze --solution <root-solution.sln> --rules .codepass/rules --output codepass-quality.json --fail-on-rule-warnings false`

## Styling and assets

- Use Tailwind CSS classes and Tailwind tooling/configuration.
- Do not put custom CSS or JavaScript inline in Razor files.
- Do not implement the app as a single monolithic Razor/CSS/JS file.
- Keep CSS/JS assets organized in normal project files.

## Documentation and containers

- Add a `README.md` with setup, local run, test, Docker, and docker-compose instructions.
- Add a real `Dockerfile` at the workspace root. It must build and publish the app; it cannot be a placeholder.
- Add a `docker-compose.yml` or `compose.yml` at the workspace root that builds and runs the app.

## Tests

Add automated tests for each required component. The tests should verify rendering and key interactions, not just compile the project.

Before finishing, run `dotnet list package --vulnerable --include-transitive` and update or remove vulnerable package versions if any are reported.

Only public validation is visible in this workspace. Hidden validation will run after your changes.
