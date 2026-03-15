**Task:** Continue and complete the refactor process. Some refactoring has already been done;

### 1. Analyze code quality
- Identify code duplications and extract shared logic into reusable methods or types where appropriate. 
- Find complex, long, or hard-to-read functions and plan how to break them into smaller, single-responsibility methods. 
- Flag large classes or modules that are taking too many responsibilities. 

### 2. Plan and perform refactoring
- Create a step-by-step refactor plan for the remaining non-refactored areas (do not rewrite already improved parts).  
- Apply **SOLID** principles (Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, Dependency Inversion) to improve design and maintainability. 
- Simplify logic and improve naming so the intent of the code is clear and easy to follow.

### 3. Testing and coverage
- Check that all non-UI code has adequate unit test coverage (critical paths, edge cases, and error handling). 
- Where coverage is missing or weak, add or extend unit tests to cover the refactored behavior.  
- Keep tests fast, isolated, and focused on a single behavior per test. 

### 4. Project and folder structure
- Ensure each project file (`*.csproj`) lives in a folder that matches the project name.  
  - Example: `Financial.Domain.csproj` must be in folder `Financial.Domain`.  
- All test projects must live under a top-level `Tests` folder.  
- Each test project must mirror its corresponding main project by name, with `.Tests` appended.  
  - Example: main project `Financial.Domain.csproj` → test project `Financial.Domain.Tests.csproj` inside `Tests`.  
- Inside `Tests`, mirror the same project and folder structure as the projects being tested so it is easy to find tests for any given project. 

### 5. Important constraints
- Respect and reuse the parts that are already refactored; do not undo or duplicate completed work.  
- When in doubt, prefer small, incremental changes that keep the code compiling and tests passing at each step.


# Task 2

Re arange the folders on the project


# Task 3

Identify instrument type for each asset, 
FIIs, REIT, ETF, SHARES, ACOES, Pensao privada, Fundos