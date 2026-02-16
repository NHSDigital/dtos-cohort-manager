# Git Branching Strategies

## Trunk-based Development

At its most simple, trunk-based development consists of a single branch (`main`) where all developers commit their changes on an ongoing basis. This approach emphasizes continuous integration and frequent commits to the main branch, which can lead to faster feedback and quicker releases.

### Example Trunk-Based Development Workflow

In the example below, the commits from two developers (Dev A and Dev B) are merged directly into the `main` branch. Pull requests are not used, and the focus is on continuous integration and frequent commits to the main branch.

```mermaid
%%{init: {'theme':'base', 'themeVariables': { 'git0':'#3b82f6','git1':'#10b981','git2':'#f59e0b','git3':'#10b981','git4':'#f59e0b'}}}%%
gitGraph

    checkout main
    commit
    commit

    branch devA-1
    checkout devA-1
    commit

    checkout main
    merge devA-1

    branch devB-1
    checkout devB-1
    commit

    checkout main
    merge devB-1

    branch devA-2
    checkout devA-2
    commit

    checkout main
    branch devB-2
    checkout devB-2
    commit

    checkout main
    merge devA-2

    checkout main
    merge devB-2
    commit
```

### Trunk-Based Development Downsides

While trunk-based development can lead to faster feedback and quicker releases, it can also lead to issues with code quality if not managed properly. Without pull requests and code reviews, it can be easier for bugs and issues to be introduced into the main branch, which can impact the overall quality of the codebase. Additionally, if multiple developers are working on different features in parallel, it can lead to conflicts and merge issues when changes are merged into the main branch.

## GitHub Flow (Feature Branching with Pull Requests)

GitHub Flow is a simple branching strategy based on Trunk-based Development, but with the addition of pull requests for code review and collaboration. In GitHub Flow, developers create feature branches off of the main branch, work on their changes, and then open a pull request to merge their changes back into the main branch. This allows for code review and discussion before changes are merged, which can help improve code quality and catch issues early.

> This is the approach currently in use in Cohort Manager, and is *mostly* a good fit as it provides a simple branching strategy while still incorporating code review and collaboration opportunities.

### Example GitHub Flow Workflow

In the example below, developers create **feature branches** off of the `main` branch, work on their changes, and then open pull requests to merge their changes back into the `main` branch. This allows for code review and discussion before changes are merged.

```mermaid
%%{init: {'theme':'base', 'themeVariables': { 'git0':'#3b82f6','git1':'#10b981','git2':'#f59e0b','git3':'#10b981','git4':'#f59e0b'}}}%%
gitGraph

    checkout main
    commit
    commit

    branch devA-Feature-1
    checkout devA-Feature-1
    commit
    commit
    commit
    checkout main

    branch devB-Feature-2
    checkout devB-Feature-2
    commit
    commit

    checkout main
    merge devA-Feature-1 id: "PR-01: Feature A"

    checkout main
    branch devA-Feature-3
    checkout devA-Feature-3
    commit
    commit

    checkout main
    merge devA-Feature-3  id: "PR-02: Feature C"

    checkout main
    merge devB-Feature-2 id: "PR-03: Feature B"
    
```

### GitHub Flow Downsides

While GitHub Flow provides a simple and effective branching strategy, it is not an ideal option when multiple developers are working on different features in parallel as it can lead to a large number of feature branches and pull requests.

Additionally, if pull requests are not reviewed and merged in a timely manner (for instance while assurance processes are undertaken), it can lead to long-lived branches that diverge significantly from the `main` branch, making merging more difficult and increasing the risk of conflicts.

## Git Flow

Git Flow is a more structured branching strategy that defines specific branches for different purposes, such as development, releases, and hotfixes. It provides a clear workflow paticularly suited to teams with multiple developers working on different features in parallel and with fixed release cycles. 


### Example Git Flow Workflow

In the example below, developers create **feature branches** off the `develop` branch, work on their changes, and then open pull requests to merge their changes back into the `develop` branch. When a release is ready, a `release` branch is created from `develop`, and once the release is deployed, it is merged back into both `main` and `develop`.

If a hotfix is required before the next release, a `hotfix` branch is created from `main`, the fix is made, and then the hotfix branch is merged back into both `main` and `develop` to ensure the fix is included in future releases.

```mermaid
%%{init: {'theme':'base', 'themeVariables': { 'git0':'#3b82f6','git1':'#10b981','git2':'#f50b0b','git3':'#f59e0b','git4':'#a855f7','git5':'#fe45be'}}}%%
gitGraph

    checkout main
    commit

    branch develop
    branch hotfix
    
    checkout develop
    commit

    branch feature-A

    checkout main
    checkout hotfix
    commit id: "Hotfix for v1.0.1"

    checkout main
    merge hotfix id: "Hotfix v1.0.1"

    checkout feature-A
    commit
    commit

    checkout develop
    merge feature-A id: "PR: Feature A"

    branch feature-B
    checkout feature-B
    commit
    commit

    checkout develop
    merge hotfix id: "Hotfix v1.0.1 (dev)"

    checkout develop
    merge feature-B id: "PR: Feature B"

    branch release-1.0
    checkout release-1.0
    commit id: "v1.0.0"

    checkout main
    merge release-1.0 id: "Deploy v1.0"

    checkout develop
    merge release-1.0

```

### Git Flow Downsides

Due to its branch complexity, Git Flow can be more difficult to manage and may require more overhead in terms of branch management and merging.

In Cohort Manager, we would also need to consider how Git Flow would fit in with our current CI/CD pipeline, particularly on how container images are built annd the ability to push unmerged changes to Azure via the DevTest workflow, and whether accomodating these factors would require significant changes to our existing processes.

## Potential Alternative: GitHub Flow with Release Branches

An alternative approach that combines elements of both GitHub Flow and Git Flow is to use GitHub Flow for feature development and pull requests, but also incorporate release branches to provide a backstop for releases and allow for hotfixes if needed. This approach allows for the simplicity and collaboration of GitHub Flow while also providing the structure and stability of release branches.

### Example GitHub Flow with Release Branches Workflow

In the example below, developers create **feature branches** off the `main` branch, work on their changes, and then open pull requests to merge their changes back into the `main` branch. When one or more PRs are ready for release, a `release` branch is created from `main` to act as a backstop incase changes need to be pulled out of the release, or hotfixes are needed while the release is in progress. The release branch does ot need to be merged back into `main` as the release branch is created from `main` and only contains changes that have already been merged into `main`.

```mermaid
%%{init: {'theme':'base', 'themeVariables': { 'git0':'#3b82f6','git1':'#10b981','git2':'#f59e0b','git3':'#10b981','git4':'#fe45be','git5':'#fe45be'}}}%%
gitGraph

    checkout main
    commit
    commit

    branch devA-Feature-1
    checkout devA-Feature-1
    commit
    commit
    commit
    checkout main

    branch devB-Feature-2
    checkout devB-Feature-2
    commit
    commit

    checkout main
    merge devA-Feature-1 id: "PR-01: Feature A"

    checkout main
    branch devA-Feature-3
    checkout devA-Feature-3
    commit
    commit

    checkout main
    merge devA-Feature-3  id: "PR-02: Feature C"
    
    branch release-1.0
    checkout release-1.0
    commit id: "Release A+C"

    checkout main
    merge devB-Feature-2 id: "PR-03: Feature B"
    
```

### Applying a Hotfix to a Release Branch

If a hotfix is needed while a release is in progress, a hotfix branch can be created from the release branch, the fix can be made, and then the hotfix branch can be merged back into the release branch to ensure the fix is included in the release.

Imagine the case where the following sequence of events occurs:

```mermaid
flowchart TD
    A1[Release-1.0 branch created from main prior to new changes being developed] --> A2[Release-1.0 released to production]
    A2 --> B1[Developer A starts Feature A]
    B1 --> C1[Developer B starts Feature 2]
    C1 --> B2[Developer A completes Feature A, which is merged into main]
    B2 --> D1[Developer A starts Feature C]
    D1 --> D2[Developer A completes Feature C, which is merged into main but not released]
    C1 --> E1[Hotfix-1 is required to resolve a critical issue in the release]
    E1 --> E2[Commit hotfix changes to release-1.0 branch]
    E2 --> E3[Rebuild and deploy release-1.0 with hotfix to production]
    D2 --> E4[Merge hotfix-1 into main branch to ensure fix is included in future releases]
    E4 --> F1[Release-1.1 branch created from main to include new features and hotfix-1 for next release]
    F1 --> F2[Release-1.1 built and released to production]
    F2 --> G1[Hotfix-2 is required to resolve a critical issue in release-1.1]
    G1 --> G2[Commit hotfix changes to release-1.1 branch]
    G2 --> G3[Rebuild and deploy release-1.1 with hotfix to production]
    G3 --> G4[Merge hotfix-2 into main branch to ensure fix is included in future releases]
    G4 --> C2[Developer B completes Feature 2, which is merged into main but not released]

    style A1 fill:#fe45be
    style A2 fill:#3b82f6
    style B1 fill:#10b981
    style B2 fill:#10b981
    style C1 fill:#f59e0b
    style C2 fill:#f59e0b
    style D1 fill:#10b981
    style D2 fill:#10b981
    style E1 fill:#f50b0b
    style E2 fill:#f50b0b
    style E3 fill:#f50b0b
    style E4 fill:#3b82f6
    style F1 fill:#fe45be
    style F2 fill:#3b82f6
    style G1 fill:#f50b0b
    style G2 fill:#f50b0b
    style G3 fill:#f50b0b
    style G4 fill:#3b82f6
```

```mermaid
%%{init: {'theme':'base', 'themeVariables': {'git0':'#3b82f6', 'git1':'#fe45be','git2':'#f50b0b','git3':'#10b981','git4':'#f59e0b','git5':'#10b981','git6':'#fe45be', 'git7':'#f50b0b'}}}%%
gitGraph

    checkout main
    commit

    branch release-1.0
    commit
    
    branch hotfix-1
    checkout hotfix-1
    commit id: "Hotfix 1 for main"
    checkout release-1.0
    merge hotfix-1 id: "Hotfix 1 for release-1.0"

    checkout main
    commit

    branch devA-Feature-1
    checkout devA-Feature-1
    commit
    commit
    commit
    checkout main

    branch devB-Feature-2
    checkout devB-Feature-2
    commit
    commit

    checkout main
    merge devA-Feature-1 id: "PR-01: Feature A"


    checkout main
    branch devA-Feature-3
    checkout devA-Feature-3
    commit
    commit

    checkout main
    merge devA-Feature-3  id: "PR-02: Feature C"
    
    checkout main
    merge hotfix-1 id: "PR-04 Hotfix for main"
    commit

    branch release-1.1
    checkout release-1.1
    commit id: "Release 1.1"
    
    branch hotfix-2
    checkout hotfix-2
    commit id: "Hotfix for Release 1.1"
    checkout release-1.1
    merge hotfix-2 id: "Hotfix for Release 1.1 (release branch)"

    checkout main
    merge hotfix-2 id: "Hotfix for Release 1.1 (main)"

    checkout main
    merge devB-Feature-2 id: "PR-03: Feature B"
    
```
