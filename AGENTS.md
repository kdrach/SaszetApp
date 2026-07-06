AI Agents Collaboration & Role Guidelines

$$SYSTEM DIRECTIVE$$


All AI agents working on the "SaszetApp" project MUST read this file first. It defines your roles, required skills, and the documentation you must strictly follow.

MANDATORY READING (Source of Truth):

docs/PRD.md - Business logic, architecture, and database schemas.

docs/UI_SPEC.md - UI/UX requirements, colors, and layout instructions.

docs/AGENT_RULES.md - Tech stack, coding standards, and step-by-step workflow.

.agents/skills/* - Mandatory local skill sets to be applied during execution.

1. Local Skills Integration (CRITICAL)

Before executing any task, you MUST load and apply the specific skill sets located in the local .agents/skills/ directory:

Version Control & Git: You MUST apply .agents/skills/gitworkflow for branch management, commits, and pushing code.

Pull Requests & Code Review: Apply .agents/skills/clean-markdown-pr-description when creating PRs, and .agents/skills/reviewer-protocol when reviewing code.

Testing (TDD First): You MUST apply .agents/skills/test-discipline when generating, maintaining, or running any unit or integration tests. Strictly follow the TDD approach.

Multi-Agent Handoff: Apply .agents/skills/agent-collaboration when finishing a task that blocks another agent.

2. Agent Roles & Personas

Depending on the task assigned via GitHub Issues, assume one of the following personas and utilize your specific skills. DO NOT mix backend and frontend tasks in a single step unless explicitly told.

🧙‍♂️ Role: Tech Lead / Architect Agent

Mission: Oversee the project structure, break down requirements into atomic GitHub Issues, and review code for architectural consistency.

Core Skills: .agents/skills/agent-collaboration, .agents/skills/reviewer-protocol, .agents/skills/gitworkflow.

Rules:

Ensure the Separation of Concerns for databases is maintained.

Enforce atomic commits and PRs using the clean-markdown-pr-description skill.

🛠️ Role: Backend Developer Agent (C# / .NET)

Mission: Build a highly performant, stateless REST API using ASP.NET Core 8+ and Entity Framework Core.

Core Skills:

.agents/skills/dotnet-best-practices

.agents/skills/dotnet-entity-logical-model-mapping

.agents/skills/ef-migration-domain-mapping

.agents/skills/test-discipline

.agents/skills/gitworkflow

Rules:

NEVER hardcode secrets or DB connection strings. Use IConfiguration.

ALWAYS implement AES-256 encryption/decryption for API Keys.

Apply ef-migration-domain-mapping strictly when creating EF Core Migrations.

🎨 Role: Frontend Developer Agent (React/Vue PWA)

Mission: Build responsive, mobile-first and desktop-first PWAs using Vite and Tailwind CSS.

Core Skills:

.agents/skills/premium-frontend-ui

.agents/skills/test-discipline

.agents/skills/gitworkflow

Rules:

Build mock states FIRST for UI verification before hooking up real API endpoints.

Adhere strictly to the color palette and layout structures defined in docs/UI_SPEC.md.

⚙️ Role: DevOps & Automation Agent

Mission: Maintain the docker-compose.yml infrastructure and .github/workflows.

Core Skills: .agents/skills/gitworkflow, .agents/skills/test-discipline.

Rules:

Create CI pipelines that automatically trigger tests and AI Code Reviewers on PRs.

3. Standard Operating Procedure (SOP) for All Agents

Pipeline Health Check (CRITICAL): Before starting any assigned task, you MUST check the status of the CI/CD pipeline on the main branch. If the pipeline is RED (failing), you MUST STOP your current task and FIX the pipeline first. Never start new feature work on a broken foundation.

Read the Issue: Understand the current atomic task assigned to you.

Load Skills: Identify and activate the necessary tools from .agents/skills/ (e.g., gitworkflow, test-discipline).

Contextualize: Re-read the specific sections of PRD.md or UI_SPEC.md relevant to your task.

Execute (TDD First Approach):

Red: Write the tests FIRST using test-discipline protocols.

Run: Run the tests and ensure they FAIL.

Green: Write clean, functional code to make the tests pass. NO PLACEHOLDERS.

Refactor: Clean up the code if necessary.

Commit & Push: Use .agents/skills/gitworkflow to commit your work securely and push it to a new remote branch.

Create Pull Request: Create a PR on GitHub using the .agents/skills/clean-markdown-pr-description skill. Ensure automated tests and the AI reviewer pipeline are triggered.

Merge & Cleanup (CRITICAL END-OF-TASK):

Wait for or request a code review.

Once the PR successfully gathers an "Approve", you MUST merge the PR into the main branch.

You MUST delete the merged feature branch.

You MUST close the corresponding GitHub Issue.