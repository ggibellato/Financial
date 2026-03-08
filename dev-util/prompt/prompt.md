# **Purpose**  
This is the main (master) prompt for the **Financial Project**. It defines the overall rules for how the system should operate and specifies which additional prompts and agents to use for specialized tasks.  

**Project Overview**  
The project is a **personal financial management tool** designed to consolidate all financial transactions across multiple investment accounts.  

The user’s current portfolio spans **two countries** — **Brazil** and the **United Kingdom** — which introduces two different currencies and distinct tax regulations.  

The system’s goals include:  
- Assisting with **annual tax reporting** in both jurisdictions.  
- Tracking the **performance and returns** of each investment.  
  
The portfolio covers various asset classes, including **Bitcoin, REITs (fundos imobiliários in Brazil), shares, ETFs, government bonds, and ISAs**. This list is not exhaustive and may expand over time as additional investment types are supported.  

Always ensuring that the **business logic** and **data access** layers are isolated from the presentation layer so that the tool can use different  frontends (web, mobile apps, etc.) without duplicating logic.

# **Behaviour**
Whenever you need to create new `.md` files, save them under `Financial/dev-util/prompt/md/`.
Follow the behaviour defined in `Financial/dev-util/prompt/behaviour.md`.

# **Role**
The role configuration is defined in Financial\dev-util\prompt\csharp.md
Refer to that document for language-specific behavior, code conventions, and development guidelines.

# **Code analyzer**
If discovery-findings.md does exist, read its contents and incorporate the information into the current context.

If the file Financial\dev-util\prompt\md\discovery-findings.md does not exist, execute the process defined in Financial\dev-util\prompt\codebase-analyzer-agent.md to perform a codebase scan and generate findings and store the file information Financial\dev-util\prompt\md\discovery-findings.md

Here’s a cleaned-up version that should be easier for Copilot (and humans) to follow:

***

# Workflow

1 - Start by follow what is on the Code analyzer section
2 - Follow the task.md to know what need to be done
3 - Keep a self record of which tasks has been completed and relevant information as this project it will use many sessions
