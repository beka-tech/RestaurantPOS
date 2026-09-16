# RestaurantPOS Git Workflow Guide

A simple, repeatable Git workflow for working on the **RestaurantPOS** project without creating unrelated branch histories.

---

## 1. The Mental Model

Think of your Git workflow like this:

```text
main
 │
 ├── create feature branch
 │       ↓
 │     code
 │       ↓
 │     git add
 │       ↓
 │     git commit
 │       ↓
 │     git push
 │       ↓
 │      PR
 │       ↓
 │     merge
 │
 └── update main
```

The most important rule is:

> **Every new feature branch should start from an updated `main` branch.**

---

# 2. Start Every New Feature from `main`

Before you begin any new work:

```bash
git switch main
git pull origin main
```

This makes sure your local `main` contains the latest changes from GitHub.

### Mental model

```text
GitHub main
     │
     ▼
Local main
     │
     ▼
New feature branch
```

---

# 3. Create a Feature Branch

After updating `main`, create a branch for the work you are about to do.

Example:

```bash
git switch -c feat/domain-entities
```

Other examples:

```bash
git switch -c feat/payment-entity
git switch -c feat/order-entity
git switch -c feat/menu-management
git switch -c test/domain-entities
git switch -c fix/table-session-close
```

---

# 4. Branch Naming Convention

Use consistent branch prefixes.

| Type | Purpose | Example |
|---|---|---|
| `feat/` | New feature | `feat/payment-entity` |
| `fix/` | Bug fix | `fix/table-session-close` |
| `refactor/` | Code restructuring | `refactor/order-lifecycle` |
| `test/` | Tests | `test/table-session` |
| `docs/` | Documentation | `docs/domain-model` |
| `chore/` | Maintenance/configuration | `chore/update-gitignore` |

### RestaurantPOS examples

```text
feat/domain-entities
feat/order-management
feat/payment-workflow
feat/menu-management

fix/table-session-close
fix/payment-validation

test/domain-entities
test/payment-workflow

refactor/order-lifecycle

docs/domain-model
```

---

# 5. Confirm Which Branch You Are On

Before coding, check your current branch:

```bash
git branch
```

Example output:

```text
  main
* feat/domain-entities
```

The `*` means:

> You are currently working on `feat/domain-entities`.

You can also run:

```bash
git status
```

---

# 6. Write Your Code

Example RestaurantPOS domain structure:

```text
src/
└── RestaurantPOS.Domain/
    ├── Common/
    │   └── Entity.cs
    │
    ├── Entities/
    │   ├── RestaurantTable.cs
    │   ├── TableSession.cs
    │   ├── Order.cs
    │   ├── OrderItem.cs
    │   ├── Payment.cs
    │   └── Receipt.cs
    │
    └── Enums/
        ├── TableStatus.cs
        ├── TableSessionStatus.cs
        ├── OrderStatus.cs
        └── PaymentStatus.cs
```

While working, check what changed:

```bash
git status
```

Example:

```text
modified:
    src/RestaurantPOS.Domain/Entities/TableSession.cs

untracked:
    src/RestaurantPOS.Domain/Entities/Order.cs
```

---

# 7. Stage Your Changes

To stage everything:

```bash
git add .
```

Then check:

```bash
git status
```

You should see something similar to:

```text
Changes to be committed:

    modified:   TableSession.cs
    new file:   Order.cs
```

## Safer option: add specific files

As the project grows, prefer staging only the files related to the commit.

Example:

```bash
git add src/RestaurantPOS.Domain/Entities/Order.cs
```

This helps keep commits focused.

---

# 8. Commit Your Changes

Use short, meaningful commit messages.

Example:

```bash
git commit -m "feat: add Order entity"
```

More examples:

```bash
git commit -m "feat: add RestaurantTable entity"

git commit -m "feat: add TableSession lifecycle"

git commit -m "feat: add Payment entity"

git commit -m "fix: prevent closing unpaid table session"

git commit -m "test: add TableSession lifecycle tests"

git commit -m "refactor: simplify order lifecycle"
```

## Commit message format

```text
type: short description
```

Examples:

```text
feat: add Payment entity
fix: validate payment amount
test: add Payment entity tests
refactor: simplify table session lifecycle
docs: document domain entities
```

---

# 9. You Can Make Multiple Commits on One Feature Branch

You do **not** need a new branch for every `.cs` file.

Suppose you are on:

```text
feat/order-management
```

You can make several related commits:

```bash
git add .
git commit -m "feat: add Order entity"
```

Continue working:

```bash
git add .
git commit -m "feat: add OrderItem entity"
```

Continue again:

```bash
git add .
git commit -m "feat: add order lifecycle methods"
```

Your history could look like:

```text
main
 │
 ●
 │
 └──────── feat/order-management
             │
             ● feat: add Order entity
             │
             ● feat: add OrderItem entity
             │
             ● feat: add order lifecycle methods
```

This is normal.

---

# 10. Push Your Branch

## First push for a new branch

Use:

```bash
git push -u origin feat/order-management
```

The `-u` connects your local branch with the branch on GitHub.

After that, future pushes are simply:

```bash
git push
```

### Quick memory

```text
First push:

git push -u origin branch-name


Later pushes:

git push
```

---

# 11. Create a Pull Request

On GitHub, compare your branch against `main`.

```text
base:    main
compare: feat/order-management
```

Example PR title:

```text
feat: add Order domain model
```

The flow is:

```text
feat/order-management
        │
        ▼
   Pull Request
        │
        ▼
      main
```

Before merging, review:

- Files changed
- Commit messages
- Build status
- Tests
- Accidental files
- Merge conflicts

---

# 12. Merge the Pull Request

For small feature branches, **Squash and merge** is a good default.

Example branch history:

```text
feat/order-management

● add Order
● add OrderItem
● fix formatting
● rename method
● fix validation
```

After squash merging into `main`:

```text
main

● feat: add Order domain model
```

This keeps `main` clean and easy to read.

---

# 13. After the PR Is Merged

Your GitHub `main` now contains the feature, but your local `main` may still be behind.

Run:

```bash
git switch main
git pull origin main
```

Then delete the old local feature branch:

```bash
git branch -d feat/order-management
```

If GitHub offers a **Delete branch** button after merging, you can delete the remote branch too.

---

# 14. Start the Next Feature

Repeat the same workflow:

```bash
git switch main
git pull origin main

git switch -c feat/payment-workflow
```

Then:

```text
code
 ↓
add
 ↓
commit
 ↓
push
 ↓
PR
 ↓
merge
```

---

# 15. Complete Daily Workflow

This is the main workflow worth memorizing.

```bash
# 1. Go to main
git switch main

# 2. Get latest code
git pull origin main

# 3. Create a new feature branch
git switch -c feat/payment-workflow

# 4. Work on the code...

# 5. Check changes
git status

# 6. Stage changes
git add .

# 7. Commit
git commit -m "feat: add Payment entity"

# 8. First push
git push -u origin feat/payment-workflow
```

Continue working on the same branch:

```bash
git add .
git commit -m "feat: add payment verification rules"
git push
```

Then:

```text
GitHub
  │
  ▼
Create PR
  │
  ▼
Review
  │
  ▼
Merge
```

After the merge:

```bash
git switch main
git pull origin main
git branch -d feat/payment-workflow
```

---

# 16. Recommended RestaurantPOS Branch Strategy

Do not create one branch for every single entity unless that entity is a large independent feature.

For the initial Domain Layer, this is reasonable:

```text
main
 │
 ├── feat/domain-entities
 │     ├── RestaurantTable
 │     ├── TableSession
 │     ├── Order
 │     ├── OrderItem
 │     ├── Payment
 │     └── Receipt
 │
 ├── test/domain-entities
 │
 ├── feat/application-layer
 │
 ├── feat/infrastructure-layer
 │
 └── feat/api-layer
```

Another good approach is to split larger domain areas:

```text
feat/table-management
feat/order-management
feat/payment-workflow
feat/menu-management
feat/user-management
```

Use whichever keeps each Pull Request understandable and focused.

---

# 17. Important Mistake to Avoid

Once you have cloned your repository:

```bash
git clone <repository-url>
```

do **not** run:

```bash
git init
```

again inside that repository.

Also avoid creating a completely separate local repository and pushing it into the same GitHub repository as if it shared the same history.

## Correct structure

```text
GitHub RestaurantPOS
        ↕
Local RestaurantPOS
        │
        ├── main
        ├── feat/domain-entities
        ├── feat/order-management
        ├── feat/payment-workflow
        └── test/domain-entities
```

## Wrong structure

```text
GitHub Repository
History A
    │
    ●


Local Repository
History B
    │
    ●
```

That can cause GitHub to show:

```text
There isn't anything to compare.

main and feature/... are entirely different commit histories.
```

---

# 18. If You Already Have a Branch and Want to Continue Working

Suppose the branch already exists locally:

```bash
git switch feat/domain-entities
```

Make your changes.

Then:

```bash
git status
git add .
git commit -m "feat: add Order entity"
git push
```

You do **not** recreate the branch.

---

# 19. If the Branch Exists on GitHub but Not Locally

Fetch remote branches:

```bash
git fetch origin
```

Then switch to it:

```bash
git switch --track origin/feat/domain-entities
```

Now you can continue normally:

```bash
git add .
git commit -m "feat: add Order entity"
git push
```

---

# 20. If `main` Changed While You Were Working

Imagine someone merged another PR while you were still working on your feature.

Update `main`:

```bash
git switch main
git pull origin main
```

Return to your feature branch:

```bash
git switch feat/order-management
```

Bring the latest `main` into it:

```bash
git merge main
```

If there are no conflicts, continue:

```bash
git push
```

Then your feature branch contains the latest `main`.

---

# 21. Quick Command Reference

| Goal | Command |
|---|---|
| Check current branch | `git branch` |
| Check changed files | `git status` |
| Switch to main | `git switch main` |
| Update main | `git pull origin main` |
| Create branch | `git switch -c feat/name` |
| Switch branch | `git switch feat/name` |
| Stage everything | `git add .` |
| Stage one file | `git add path/to/file` |
| Commit | `git commit -m "feat: description"` |
| First push | `git push -u origin feat/name` |
| Later push | `git push` |
| Fetch remote changes | `git fetch origin` |
| Delete local branch | `git branch -d feat/name` |
| View short history | `git log --oneline` |
| View branch graph | `git log --oneline --graph --all` |

---

# 22. The Workflow to Memorize

```text
                  START
                    │
                    ▼
                  main
                    │
                    ▼
              pull latest
                    │
                    ▼
             create branch
                    │
                    ▼
                  code
                    │
                    ▼
               git add
                    │
                    ▼
              git commit
                    │
                    ▼
               git push
                    │
                    ▼
              Pull Request
                    │
                    ▼
                  Merge
                    │
                    ▼
             switch to main
                    │
                    ▼
               pull again
                    │
                    ▼
              NEXT FEATURE
```

---

# 23. Golden Rules

1. **Always create new branches from an updated `main`.**
2. **Do not run `git init` again inside an already cloned repository.**
3. **Use one branch per logical feature, not necessarily one branch per file.**
4. **Commit small, related changes with meaningful messages.**
5. **Push your feature branch, not directly to `main`.**
6. **Use Pull Requests to review and merge changes.**
7. **After merging, return to `main` and pull the latest code.**
8. **Delete finished feature branches when they are no longer needed.**

---

# RestaurantPOS Example

A healthy Git history could look like this:

```text
main
 │
 ● chore: initialize RestaurantPOS solution
 │
 ● feat: add domain foundation
 │
 ● feat: add table management domain
 │
 ● feat: add order management domain
 │
 ● feat: add payment workflow
 │
 ● test: add domain lifecycle tests
```

And while actively developing:

```text
main
 │
 ●
 │
 ├──────── feat/menu-management
 │           │
 │           ● add MenuCategory
 │           ● add MenuItem
 │
 └──────── feat/payment-workflow
             │
             ● add Payment
             ● add payment verification rules
```

That is the Git structure you want to maintain as the RestaurantPOS project grows.
