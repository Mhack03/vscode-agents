# Multi-Agent Development System

A production-grade orchestrated agent system that breaks down complex development tasks into specialized workflows with adaptive escalation, quality gates, and requirement clarification.

> 💡 **Tip:** Start with the **Orchestrator** agent - it will automatically delegate to the right specialists!

## Agent Architecture

```mermaid
graph TB
    User[👤 User Request]
    
    User --> Clarifier
    
    subgraph "Entry Point"
        Clarifier["🔍 Clarifier<br/>Claude Sonnet 4.5<br/><i>Seeks clarification</i>"]
    end
    
    Clarifier --> Orchestrator
    
    subgraph "Coordination Layer"
        Orchestrator["🎯 Orchestrator<br/>Claude Sonnet 4.5<br/><i>Delegates & coordinates</i>"]
    end
    
    Orchestrator --> Planner
    
    subgraph "Planning Layer"
        Planner["📋 Planner<br/>GPT-5.2<br/><i>Creates strategies</i>"]
    end
    
    Orchestrator --> DevTeam
    Orchestrator --> Designer
    Orchestrator --> DataEng
    Orchestrator --> Specialist
    
    subgraph "Development Team - Adaptive Escalation"
        DevTeam["👨‍💻 Developer Agents"]
        Junior["⚡ Junior Developer<br/>Gemini 3 Flash<br/><i>Quick fixes, simple tasks</i>"]
        
        Frontend["🎨 Frontend Dev<br/>Gemini 3 Pro<br/><i>UI, components</i>"]
        Backend["⚙️ Backend Dev<br/>Claude Opus 4.6<br/><i>APIs, databases</i>"]
        Fullstack["🔄 Fullstack Dev<br/>Gemini 3 Pro<br/><i>End-to-end features</i>"]
        
        SrFrontend["🚀 Senior Frontend<br/>GPT-5.2-Codex<br/><i>Complex UI architecture</i>"]
        SrBackend["💎 Senior Backend<br/>Claude Opus 4.6<br/><i>Distributed systems</i>"]
        SrFullstack["🏆 Senior Fullstack<br/>GPT-5.2-Codex<br/><i>Complex integrations</i>"]
        
        DevTeam --> Junior
        DevTeam --> Frontend
        DevTeam --> Backend
        DevTeam --> Fullstack
        
        Frontend -.escalate.-> SrFrontend
        Backend -.escalate.-> SrBackend
        Fullstack -.escalate.-> SrFullstack
    end
    
    subgraph "Design Layer"
        Designer["🎨 Designer<br/>Gemini 3 Pro<br/><i>UI/UX design</i>"]
    end
    
    subgraph "Data Layer"
        DataEng["📊 Data Engineer<br/>Claude Opus 4.6<br/><i>SQL, ETL, analytics</i>"]
    end
    
    subgraph "Specialist Agents"
        Specialist["🔧 Specialists"]
        PromptWriter["✍️ Prompt Writer<br/>Gemini 3 Pro<br/><i>LLM prompt optimization</i>"]
        DevOps["⚙️ DevOps<br/>GPT-5.2-Codex<br/><i>Git, dependencies, deployment</i>"]
    end
    
    Orchestrator --> Reviewer
    
    subgraph "Quality Gate"
        Reviewer["✅ Reviewer<br/>Claude Sonnet 4.5<br/><i>Code review, security</i>"]
    end
    
    Reviewer --> User
    
    style Clarifier fill:#e1f5ff,stroke:#01579b,stroke-width:2px
    style Orchestrator fill:#fff3e0,stroke:#e65100,stroke-width:3px
    style Planner fill:#f3e5f5,stroke:#4a148c,stroke-width:2px
    style Junior fill:#e8f5e9,stroke:#1b5e20,stroke-width:2px
    style Frontend fill:#e3f2fd,stroke:#0277bd,stroke-width:2px
    style Backend fill:#f3e5f5,stroke:#6a1b9a,stroke-width:2px
    style Fullstack fill:#fff3e0,stroke:#ef6c00,stroke-width:2px
    style SrFrontend fill:#bbdefb,stroke:#01579b,stroke-width:3px
    style SrBackend fill:#e1bee7,stroke:#4a148c,stroke-width:3px
    style SrFullstack fill:#ffe0b2,stroke:#e65100,stroke-width:3px
    style Designer fill:#fce4ec,stroke:#880e4f,stroke-width:2px
    style DataEng fill:#e0f2f1,stroke:#00695c,stroke-width:2px
    style Reviewer fill:#fff9c4,stroke:#f57f17,stroke-width:2px
```

---

## Agent Roster

| Agent | Install | Model | Primary Role |
|-------|---------|-------|--------------|
| **[🔍 Clarifier](clarifier.agent.md)** | [![Install in VS Code](https://img.shields.io/badge/VS_Code-Install-0098FF?style=flat-square&logo=visualstudiocode&logoColor=white)](https://aka.ms/awesome-copilot/install/agent?url=vscode%3Achat-agent/install%3Furl%3Dhttps%3A//raw.githubusercontent.com/simkeyur/vscode-agents/main/clarifier.agent.md)<br/>[![Install in VS Code Insiders](https://img.shields.io/badge/VS_Code_Insiders-Install-24bfa5?style=flat-square&logo=visualstudiocode&logoColor=white)](https://aka.ms/awesome-copilot/install/agent?url=vscode-insiders%3Achat-agent/install%3Furl%3Dhttps%3A//raw.githubusercontent.com/simkeyur/vscode-agents/main/clarifier.agent.md) | Claude Sonnet 4.5 | Requirements Analysis |
| **[🎯 Orchestrator](orchestrator.agent.md)** | [![Install in VS Code](https://img.shields.io/badge/VS_Code-Install-0098FF?style=flat-square&logo=visualstudiocode&logoColor=white)](https://aka.ms/awesome-copilot/install/agent?url=vscode%3Achat-agent/install%3Furl%3Dhttps%3A//raw.githubusercontent.com/simkeyur/vscode-agents/main/orchestrator.agent.md)<br/>[![Install in VS Code Insiders](https://img.shields.io/badge/VS_Code_Insiders-Install-24bfa5?style=flat-square&logo=visualstudiocode&logoColor=white)](https://aka.ms/awesome-copilot/install/agent?url=vscode-insiders%3Achat-agent/install%3Furl%3Dhttps%3A//raw.githubusercontent.com/simkeyur/vscode-agents/main/orchestrator.agent.md) | Claude Sonnet 4.5 | Coordination |
| **[📋 Planner](planner.agent.md)** | [![Install in VS Code](https://img.shields.io/badge/VS_Code-Install-0098FF?style=flat-square&logo=visualstudiocode&logoColor=white)](https://aka.ms/awesome-copilot/install/agent?url=vscode%3Achat-agent/install%3Furl%3Dhttps%3A//raw.githubusercontent.com/simkeyur/vscode-agents/main/planner.agent.md)<br/>[![Install in VS Code Insiders](https://img.shields.io/badge/VS_Code_Insiders-Install-24bfa5?style=flat-square&logo=visualstudiocode&logoColor=white)](https://aka.ms/awesome-copilot/install/agent?url=vscode-insiders%3Achat-agent/install%3Furl%3Dhttps%3A//raw.githubusercontent.com/simkeyur/vscode-agents/main/planner.agent.md) | GPT-5.2 | Strategy |
| **[⚡ Junior Developer](junior-dev.agent.md)** | [![Install in VS Code](https://img.shields.io/badge/VS_Code-Install-0098FF?style=flat-square&logo=visualstudiocode&logoColor=white)](https://aka.ms/awesome-copilot/install/agent?url=vscode%3Achat-agent/install%3Furl%3Dhttps%3A//raw.githubusercontent.com/simkeyur/vscode-agents/main/junior-dev.agent.md)<br/>[![Install in VS Code Insiders](https://img.shields.io/badge/VS_Code_Insiders-Install-24bfa5?style=flat-square&logo=visualstudiocode&logoColor=white)](https://aka.ms/awesome-copilot/install/agent?url=vscode-insiders%3Achat-agent/install%3Furl%3Dhttps%3A//raw.githubusercontent.com/simkeyur/vscode-agents/main/junior-dev.agent.md) | Gemini 3 Flash | Quick Fixes |
| **[🎨 Frontend Developer](frontend-dev.agent.md)** | [![Install in VS Code](https://img.shields.io/badge/VS_Code-Install-0098FF?style=flat-square&logo=visualstudiocode&logoColor=white)](https://aka.ms/awesome-copilot/install/agent?url=vscode%3Achat-agent/install%3Furl%3Dhttps%3A//raw.githubusercontent.com/simkeyur/vscode-agents/main/frontend-dev.agent.md)<br/>[![Install in VS Code Insiders](https://img.shields.io/badge/VS_Code_Insiders-Install-24bfa5?style=flat-square&logo=visualstudiocode&logoColor=white)](https://aka.ms/awesome-copilot/install/agent?url=vscode-insiders%3Achat-agent/install%3Furl%3Dhttps%3A//raw.githubusercontent.com/simkeyur/vscode-agents/main/frontend-dev.agent.md) | Gemini 3 Pro | Component Implementation |
| **[⚙️ Backend Developer](backend-dev.agent.md)** | [![Install in VS Code](https://img.shields.io/badge/VS_Code-Install-0098FF?style=flat-square&logo=visualstudiocode&logoColor=white)](https://aka.ms/awesome-copilot/install/agent?url=vscode%3Achat-agent/install%3Furl%3Dhttps%3A//raw.githubusercontent.com/simkeyur/vscode-agents/main/backend-dev.agent.md)<br/>[![Install in VS Code Insiders](https://img.shields.io/badge/VS_Code_Insiders-Install-24bfa5?style=flat-square&logo=visualstudiocode&logoColor=white)](https://aka.ms/awesome-copilot/install/agent?url=vscode-insiders%3Achat-agent/install%3Furl%3Dhttps%3A//raw.githubusercontent.com/simkeyur/vscode-agents/main/backend-dev.agent.md) | Claude Opus 4.6 | API Logic & CRUD |
| **[🔄 Fullstack Developer](fullstack-dev.agent.md)** | [![Install in VS Code](https://img.shields.io/badge/VS_Code-Install-0098FF?style=flat-square&logo=visualstudiocode&logoColor=white)](https://aka.ms/awesome-copilot/install/agent?url=vscode%3Achat-agent/install%3Furl%3Dhttps%3A//raw.githubusercontent.com/simkeyur/vscode-agents/main/fullstack-dev.agent.md)<br/>[![Install in VS Code Insiders](https://img.shields.io/badge/VS_Code_Insiders-Install-24bfa5?style=flat-square&logo=visualstudiocode&logoColor=white)](https://aka.ms/awesome-copilot/install/agent?url=vscode-insiders%3Achat-agent/install%3Furl%3Dhttps%3A//raw.githubusercontent.com/simkeyur/vscode-agents/main/fullstack-dev.agent.md) | Gemini 3 Pro | End-to-End Features |
| **[🚀 Senior Frontend Developer](sr-frontend-dev.agent.md)** | [![Install in VS Code](https://img.shields.io/badge/VS_Code-Install-0098FF?style=flat-square&logo=visualstudiocode&logoColor=white)](https://aka.ms/awesome-copilot/install/agent?url=vscode%3Achat-agent/install%3Furl%3Dhttps%3A//raw.githubusercontent.com/simkeyur/vscode-agents/main/sr-frontend-dev.agent.md)<br/>[![Install in VS Code Insiders](https://img.shields.io/badge/VS_Code_Insiders-Install-24bfa5?style=flat-square&logo=visualstudiocode&logoColor=white)](https://aka.ms/awesome-copilot/install/agent?url=vscode-insiders%3Achat-agent/install%3Furl%3Dhttps%3A//raw.githubusercontent.com/simkeyur/vscode-agents/main/sr-frontend-dev.agent.md) | GPT-5.2-Codex | Complex UI Architecture |
| **[💎 Senior Backend Developer](sr-backend-dev.agent.md)** | [![Install in VS Code](https://img.shields.io/badge/VS_Code-Install-0098FF?style=flat-square&logo=visualstudiocode&logoColor=white)](https://aka.ms/awesome-copilot/install/agent?url=vscode%3Achat-agent/install%3Furl%3Dhttps%3A//raw.githubusercontent.com/simkeyur/vscode-agents/main/sr-backend-dev.agent.md)<br/>[![Install in VS Code Insiders](https://img.shields.io/badge/VS_Code_Insiders-Install-24bfa5?style=flat-square&logo=visualstudiocode&logoColor=white)](https://aka.ms/awesome-copilot/install/agent?url=vscode-insiders%3Achat-agent/install%3Furl%3Dhttps%3A//raw.githubusercontent.com/simkeyur/vscode-agents/main/sr-backend-dev.agent.md) | Claude Opus 4.6 | Distributed Systems |
| **[🏆 Senior Fullstack Developer](sr-fullstack-dev.agent.md)** | [![Install in VS Code](https://img.shields.io/badge/VS_Code-Install-0098FF?style=flat-square&logo=visualstudiocode&logoColor=white)](https://aka.ms/awesome-copilot/install/agent?url=vscode%3Achat-agent/install%3Furl%3Dhttps%3A//raw.githubusercontent.com/simkeyur/vscode-agents/main/sr-fullstack-dev.agent.md)<br/>[![Install in VS Code Insiders](https://img.shields.io/badge/VS_Code_Insiders-Install-24bfa5?style=flat-square&logo=visualstudiocode&logoColor=white)](https://aka.ms/awesome-copilot/install/agent?url=vscode-insiders%3Achat-agent/install%3Furl%3Dhttps%3A//raw.githubusercontent.com/simkeyur/vscode-agents/main/sr-fullstack-dev.agent.md) | GPT-5.2-Codex | Complex Integrations |
| **[📊 Data Engineer](data-engineer.agent.md)** | [![Install in VS Code](https://img.shields.io/badge/VS_Code-Install-0098FF?style=flat-square&logo=visualstudiocode&logoColor=white)](https://aka.ms/awesome-copilot/install/agent?url=vscode%3Achat-agent/install%3Furl%3Dhttps%3A//raw.githubusercontent.com/simkeyur/vscode-agents/main/data-engineer.agent.md)<br/>[![Install in VS Code Insiders](https://img.shields.io/badge/VS_Code_Insiders-Install-24bfa5?style=flat-square&logo=visualstudiocode&logoColor=white)](https://aka.ms/awesome-copilot/install/agent?url=vscode-insiders%3Achat-agent/install%3Furl%3Dhttps%3A//raw.githubusercontent.com/simkeyur/vscode-agents/main/data-engineer.agent.md) | Claude Opus 4.6 | Analytics & Data Pipelines |
| **[🎨 Designer](designer.agent.md)** | [![Install in VS Code](https://img.shields.io/badge/VS_Code-Install-0098FF?style=flat-square&logo=visualstudiocode&logoColor=white)](https://aka.ms/awesome-copilot/install/agent?url=vscode%3Achat-agent/install%3Furl%3Dhttps%3A//raw.githubusercontent.com/simkeyur/vscode-agents/main/designer.agent.md)<br/>[![Install in VS Code Insiders](https://img.shields.io/badge/VS_Code_Insiders-Install-24bfa5?style=flat-square&logo=visualstudiocode&logoColor=white)](https://aka.ms/awesome-copilot/install/agent?url=vscode-insiders%3Achat-agent/install%3Furl%3Dhttps%3A//raw.githubusercontent.com/simkeyur/vscode-agents/main/designer.agent.md) | Gemini 3 Pro | Visual Design & Mockups |
| **[✍️ Prompt Writer](prompt-writer.agent.md)** | [![Install in VS Code](https://img.shields.io/badge/VS_Code-Install-0098FF?style=flat-square&logo=visualstudiocode&logoColor=white)](https://aka.ms/awesome-copilot/install/agent?url=vscode%3Achat-agent/install%3Furl%3Dhttps%3A//raw.githubusercontent.com/simkeyur/vscode-agents/main/prompt-writer.agent.md)<br/>[![Install in VS Code Insiders](https://img.shields.io/badge/VS_Code_Insiders-Install-24bfa5?style=flat-square&logo=visualstudiocode&logoColor=white)](https://aka.ms/awesome-copilot/install/agent?url=vscode-insiders%3Achat-agent/install%3Furl%3Dhttps%3A//raw.githubusercontent.com/simkeyur/vscode-agents/main/prompt-writer.agent.md) | Gemini 3 Pro | Prompt Engineering |
| **[⚙️ DevOps](devops.agent.md)** | [![Install in VS Code](https://img.shields.io/badge/VS_Code-Install-0098FF?style=flat-square&logo=visualstudiocode&logoColor=white)](https://aka.ms/awesome-copilot/install/agent?url=vscode%3Achat-agent/install%3Furl%3Dhttps%3A//raw.githubusercontent.com/simkeyur/vscode-agents/main/devops.agent.md)<br/>[![Install in VS Code Insiders](https://img.shields.io/badge/VS_Code_Insiders-Install-24bfa5?style=flat-square&logo=visualstudiocode&logoColor=white)](https://aka.ms/awesome-copilot/install/agent?url=vscode-insiders%3Achat-agent/install%3Furl%3Dhttps%3A//raw.githubusercontent.com/simkeyur/vscode-agents/main/devops.agent.md) | GPT-5.2-Codex | Operations |
| **[✅ Reviewer](reviewer.agent.md)** | [![Install in VS Code](https://img.shields.io/badge/VS_Code-Install-0098FF?style=flat-square&logo=visualstudiocode&logoColor=white)](https://aka.ms/awesome-copilot/install/agent?url=vscode%3Achat-agent/install%3Furl%3Dhttps%3A//raw.githubusercontent.com/simkeyur/vscode-agents/main/reviewer.agent.md)<br/>[![Install in VS Code Insiders](https://img.shields.io/badge/VS_Code_Insiders-Install-24bfa5?style=flat-square&logo=visualstudiocode&logoColor=white)](https://aka.ms/awesome-copilot/install/agent?url=vscode-insiders%3Achat-agent/install%3Furl%3Dhttps%3A//raw.githubusercontent.com/simkeyur/vscode-agents/main/reviewer.agent.md) | Claude Sonnet 4.5 | Quality Assurance |

---

## Workflow Scenarios

### Scenario 1: Simple Task (Typo Fix)

```mermaid
sequenceDiagram
    actor User
    participant C as 🔍 Clarifier<br/>Sonnet 4.5
    participant O as 🎯 Orchestrator<br/>Sonnet 4.5
    participant P as 📋 Planner<br/>GPT-5.2
    participant JD as ⚡ Junior Dev<br/>Gemini Flash
    participant R as ✅ Reviewer<br/>Sonnet 4.5
    
    User->>C: Fix typo: "Submitt" → "Submit"
    C->>O: ✓ Clear request
    O->>P: Create plan for typo fix
    P->>O: Single task: Update SubmitButton.tsx
    O->>JD: Fix typo in button label
    JD->>JD: Update file
    JD->>O: ✓ Complete
    O->>R: Review changes
    R->>O: ✓ No issues found
    O->>User: Typo fixed in submit button
```

### Scenario 2: Complex Feature (Dark Mode)

```mermaid
sequenceDiagram
    actor User
    participant C as 🔍 Clarifier<br/>Sonnet 4.5
    participant O as 🎯 Orchestrator<br/>Sonnet 4.5
    participant P as 📋 Planner<br/>GPT-5.2
    participant D as 🎨 Designer<br/>Gemini Pro
    participant FE as 🎨 Frontend Dev<br/>Gemini Pro
    participant SFE as 🚀 Senior Frontend<br/>Codex
    participant R as ✅ Reviewer<br/>Sonnet 4.5
    
    User->>C: Add dark mode to the app
    C->>O: ✓ Clear - theme toggle with persistence
    O->>P: Create dark mode implementation plan
    
    P->>O: Phase 1: Design<br/>Phase 2: Core (context, toggle)<br/>Phase 3: Apply theme
    
    Note over O,D: Phase 1 - Design
    par Design Tasks (Parallel)
        O->>D: Create dark mode color palette
        O->>D: Design toggle UI component
    end
    D->>O: ✓ Design complete
    
    Note over O,SFE: Phase 2 - Core Implementation
    par Core Tasks (Parallel)
        O->>FE: Implement theme context
        FE->>FE: Working on persistence...
        FE->>O: ⚠️ Struggling with edge cases
        O->>SFE: Complete theme context (escalated)
        SFE->>O: ✓ Context complete with persistence
    and
        O->>FE: Create toggle component
        FE->>O: ✓ Toggle complete
    end
    
    Note over O,SFE: Phase 3 - Apply
    O->>SFE: Apply theme to all components
    SFE->>O: ✓ Theme applied
    
    O->>R: Review implementation
    R->>O: ✓ Passed - no issues
    O->>User: Dark mode implemented with toggle
```

### Scenario 3: Ambiguous Request (Needs Clarification)

```mermaid
sequenceDiagram
    actor User
    participant C as 🔍 Clarifier<br/>Sonnet 4.5
    participant O as 🎯 Orchestrator<br/>Sonnet 4.5
    participant P as 📋 Planner<br/>GPT-5.2
    participant SBE as 💎 Senior Backend<br/>Opus 4.6
    participant Des as 🎨 Designer<br/>Gemini Pro
    participant R as ✅ Reviewer<br/>Sonnet 4.5
    
    User->>C: Make the dashboard better
    
    C->>C: Analyzing request...<br/>Too vague!
    
    C->>User: ⚠️ Need clarification:<br/><br/>**Scope:**<br/>1. Which aspects? (UI, performance, data)<br/>2. Which dashboard? (admin vs user)<br/><br/>**Priority:**<br/>1. Specific pain points?
    
    User->>C: The admin dashboard is slow.<br/>Load time is 8+ seconds.
    
    C->>O: Clarified: Optimize admin dashboard<br/>performance (target <2s load)
    
    O->>P: Create performance optimization plan
    P->>O: Phase 1: Profile<br/>Phase 2: Optimize queries<br/>Phase 3: Add caching
    
    O->>SBE: Profile admin dashboard performance
    SBE->>O: ✓ Found: N+1 queries, no caching
    
    O->>SBE: Optimize database queries
    SBE->>O: ✓ Queries optimized
    
    O->>SBE: Implement caching layer
    SBE->>O: ✓ Redis cache added
    
    O->>R: Review optimizations
    R->>R: Security check:<br/>Cache invalidation OK<br/>No data leaks
    R->>O: ✓ Passed
    
    O->>User: Dashboard optimized:<br/>Load time now ~1.2s
```

### Scenario 4: Adaptive Escalation

```mermaid
sequenceDiagram
    actor User
    participant O as 🎯 Orchestrator<br/>Sonnet 4.5
    participant JD as ⚡ Junior Dev<br/>Gemini Flash
    participant FS as 🔄 Fullstack Dev<br/>Gemini Pro
    participant SFS as 🏆 Senior Fullstack<br/>Codex
    participant SBE as 💎 Senior Backend<br/>Opus 4.6
    
    User->>O: Add user authentication
    
    Note over O,JD: Start with Junior Dev
    O->>JD: Create login form
    JD->>JD: Building form...
    JD->>O: ⚠️ Need validation logic,<br/>session management
    
    Note over O,FS: Escalate to Fullstack Dev
    O->>FS: Complete login with validation
    FS->>FS: Adding validation...
    FS->>O: ⚠️ Security concerns:<br/>token storage, XSS protection
    
    Note over O,SFS: Escalate to Senior Fullstack
    O->>SFS: Implement secure authentication
    SFS->>SFS: Working on JWT...
    SFS->>O: ⚠️ Need architecture decision:<br/>Refresh tokens, session store
    
    Note over O,SBE: Escalate to Senior Backend
    O->>SBE: Design secure auth architecture
    SBE->>SBE: Designing system with:<br/>- JWT access tokens (15min)<br/>- Refresh tokens (7 days)<br/>- Redis session store<br/>- CSRF protection
    SBE->>O: ✓ Auth system complete
    
    O->>User: Secure authentication implemented
```

---

## Key Features

### 🎯 Adaptive Escalation
- Start with lightest agent (cost-effective)
- Escalate when complexity exceeds capacity
- Monitor context usage and progress

### ⚡ Intelligent Parallelization
- File-based conflict detection
- Independent tasks run simultaneously
- Sequential execution only when needed

### 🔍 Requirement Clarification
- Clarifier agent intercepts ambiguous requests
- Targeted questions with context
- Reasonable defaults when appropriate

### ✅ Quality Gates
- Reviewer agent checks all work
- Security, bugs, performance validation
- No code reaches user without review

### 📊 Cost Optimization
- **Gemini Flash** for simple tasks (fastest, cheapest)
- **Gemini Pro** for standard work (balanced)
- **GPT-5.2-Codex** for speed-critical features
- **Claude Opus** only for critical/complex work

---

## Agent Boundaries

Clear separation of responsibilities to prevent overlap:

### 📊 Data Engineer vs ⚙️ Backend Developer

**Data Engineer** (Data-Centric Work)
- ✅ Analytical SQL queries (aggregations, window functions, complex JOINs)
- ✅ Data warehousing & ETL pipelines (Databricks, Spark, Airflow)
- ✅ Batch data processing & transformations
- ✅ Data quality validation & cleansing
- ✅ Working with data files (CSV, JSON, Parquet)
- ✅ Analytics, reporting queries, data modeling
- ❌ CRUD APIs or real-time request handling
- ❌ Application business logic
- ❌ User authentication/authorization

**Backend Developer** (Application Logic)
- ✅ REST/GraphQL API development
- ✅ Business rules & workflows
- ✅ CRUD operations for applications
- ✅ Authentication, authorization, sessions
- ✅ Real-time request/response handling
- ✅ Microservices & application integration
- ❌ Complex analytical queries or data warehousing
- ❌ Large-scale ETL pipelines
- ❌ Databricks/Spark jobs

**Rule of Thumb:** If it powers an API endpoint → Backend Developer. If it processes/analyzes bulk data → Data Engineer.

### 🎨 Designer vs 🎨 Frontend Developer  

**Designer** (Visual Design & Planning)
- ✅ Creating mockups, prototypes, wireframes
- ✅ Defining color palettes, typography, spacing systems
- ✅ Design tokens & style guides
- ✅ User experience flows & interaction patterns
- ✅ Brand guidelines & visual identity
- ❌ Writing production code (React, Vue, etc.)
- ❌ Implementing components with logic
- ❌ API integration or state management

**Frontend Developer** (Implementation)
- ✅ Implementing designs as functional components
- ✅ Writing HTML/CSS/JavaScript/TypeScript
- ✅ Component logic, state management, routing
- ✅ API integration & data fetching
- ✅ Form handling, validation, interactivity
- ❌ Creating design systems from scratch
- ❌ Making major design decisions (without Designer input)
- ❌ Visual mockups or prototypes

**Rule of Thumb:** Designer creates the blueprint → Frontend Developer builds it. Designer hands off Figma/specs → Developer writes code.

### Quick Reference: Which Agent for Common Tasks?

| Task | Correct Agent | NOT This Agent |
|------|--------------|----------------|
| "Build a REST API for user management" | ⚙️ Backend Developer | ❌ Data Engineer |
| "Write SQL query to analyze last quarter's revenue" | 📊 Data Engineer | ❌ Backend Developer |
| "Create Databricks notebook for ETL pipeline" | 📊 Data Engineer | ❌ Backend Developer |
| "Design the color scheme and layout for dashboard" | 🎨 Designer | ❌ Frontend Developer |
| "Implement the login form with validation" | 🎨 Frontend Developer | ❌ Designer |
| "Build a React component from this Figma file" | 🎨 Frontend Developer | ❌ Designer |
| "Create mockup showing user flow for checkout" | 🎨 Designer | ❌ Frontend Developer |
| "Optimize this slow Spark job processing logs" | 📊 Data Engineer | ❌ Senior Backend Developer |
| "Create microservice architecture for payments" | 💎 Senior Backend Developer | ❌ Data Engineer |

---

## Decision Matrix

### When Starting a Task

| Task Complexity | Files | Lines | Start With |
|----------------|-------|-------|------------|
| Typo fix, config change | 1-2 | <50 | ⚡ Junior Developer |
| Standard feature | 3-5 | 50-300 | 🎨 Frontend/Backend/Fullstack Developer |
| Multi-file feature, API integration | 5+ | 300-1000 | 🚀 Senior Frontend/Backend/Fullstack |
| Architecture, security, performance | Any | Any | 💎 Senior Backend Developer |

### When to Escalate

| Sign | Action |
|------|--------|
| Multiple clarification rounds | Escalate one level |
| Context usage >50% | Escalate one level |
| Security/performance concerns | Escalate to Senior Backend/Fullstack Developer |
| Agent indicates scope exceeds capability | Escalate one level |

---

## Getting Started

1. **User makes request** → Goes to Clarifier first
2. **Clarifier** → Confirms clarity or asks questions
3. **Orchestrator** → Calls Planner for strategy
4. **Planner** → Returns phased implementation plan
5. **Orchestrator** → Delegates to specialists, monitors progress
6. **Specialists** → Execute tasks (with escalation as needed)
7. **Reviewer** → Validates quality before user delivery
8. **Orchestrator** → Reports results to user

---

## Best Practices

✅ **DO:**
- Let Clarifier handle ambiguous requests
- Start with lightest appropriate agent
- Run independent tasks in parallel
- Review before finalizing
- Document assumptions when clarifying

❌ **DON'T:**
- Skip clarification for vague requests
- Over-assign (use Senior devs for simple tasks)
- Run file-overlapping tasks in parallel
- Skip review step
- Let specialists create their own documentation

---

## Model Selection Rationale

| Model | Use Case | Strengths |
|-------|----------|-----------|
| **Claude Sonnet 4.5** | Orchestration, Review, Clarification | Reasoning, coordination, analysis |
| **Claude Opus 4.6** | Backend development, Data engineering | Deep technical expertise, careful design |
| **GPT-5.2** | Planning | Strategic thinking, comprehensive plans |
| **GPT-5.2-Codex** | Senior Frontend/Fullstack, DevOps | Speed, code generation, tooling |
| **Gemini 3 Pro** | Frontend/Fullstack dev, Design, Prompt writing | Balanced performance, good at UI |
| **Gemini 3 Flash** | Junior developer tasks | Fastest execution, cost-effective |

---

## Agent Files

- [`orchestrator.agent.md`](orchestrator.agent.md) - Main coordination logic
- [`clarifier.agent.md`](clarifier.agent.md) - Requirement clarification
- [`planner.agent.md`](planner.agent.md) - Strategy creation
- [`junior-dev.agent.md`](junior-dev.agent.md) - Quick fixes and simple tasks
- [`frontend-dev.agent.md`](frontend-dev.agent.md) - UI and components
- [`backend-dev.agent.md`](backend-dev.agent.md) - APIs and databases
- [`fullstack-dev.agent.md`](fullstack-dev.agent.md) - End-to-end features
- [`sr-frontend-dev.agent.md`](sr-frontend-dev.agent.md) - Complex UI architecture
- [`sr-backend-dev.agent.md`](sr-backend-dev.agent.md) - Distributed systems
- [`sr-fullstack-dev.agent.md`](sr-fullstack-dev.agent.md) - Complex integrations
- [`data-engineer.agent.md`](data-engineer.agent.md) - SQL, ETL, and analytics
- [`designer.agent.md`](designer.agent.md) - UI/UX design
- [`prompt-writer.agent.md`](prompt-writer.agent.md) - Prompt engineering
- [`devops.agent.md`](devops.agent.md) - Operations and deployment
- [`reviewer.agent.md`](reviewer.agent.md) - Quality assurance

---

**System Rating: 10/10** ⭐⭐⭐⭐⭐⭐⭐⭐⭐⭐

A production-grade multi-agent development system with intelligent orchestration, adaptive resource allocation, comprehensive quality gates, and one-click installation.

---

## Skills & Specialized Knowledge

Agents have access to specialized skills for domain-specific guidance:

### Development Skills
- **TypeScript Patterns** - Advanced types, generics, type-safe APIs, React & Node.js patterns
- **Code Quality & Clean Code** - SOLID principles, design patterns, refactoring, code smells
- **Data Transformation & ETL** - CSV/JSON/XML parsing, validation, streaming/batch processing

### Platform & Architecture
- **Frontend Architecture** - Performance optimization, component design, state management
- **API Design** - RESTful patterns, versioning, error handling
- **Database Optimization** - Query optimization, indexing, performance tuning

### Quality & Security
- **Testing & QA** - Unit, integration, E2E testing strategies
- **Security Best Practices** - Authentication, encryption, vulnerability prevention

---

## Cost Optimization

The adaptive escalation model optimizes costs by matching agent capability to task complexity:

- **70% of tasks** → Junior Developer (Gemini Flash) - Quick fixes and simple implementations
- **20% of tasks** → Frontend/Backend/Fullstack Developers - Standard features and APIs
- **10% of tasks** → Senior Frontend/Backend/Fullstack Developers (Opus/Codex) - Complex architecture and critical systems

**Key Benefits:**
- Start with the most cost-effective agent appropriate for the task
- Escalate only when complexity demands it
- Parallel execution reduces overall time and token usage
- Mandatory clarification prevents costly rework
- Quality gates catch issues before production

This approach delivers enterprise-grade quality while keeping costs predictable and efficient. 🚀

