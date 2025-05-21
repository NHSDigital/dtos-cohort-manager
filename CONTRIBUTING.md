# Contributing

## Code/ Style Conventions

- Code should follow [Microsoft's C# code conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Classes should use [file-scoped namespaces](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-10.0/file-scoped-namespaces) and be in the format `NHS.CohortManager.<Tests>.Service` \
    i.e `NHS.CohortManager.CohortDistributionService`
- Test names should follow the convention `NameOfMethod_Scenario_ExpectedResult`

## Pull Requests

- Commit names should follow the [conventional commits standard](https://www.conventionalcommits.org/en/v1.0.0/) \
    i.e `refactor: move class out of shared`
- Branch names should follow the following format: \
    `type/DTOSS-<ticket-number>-<pr-description>` \
    i.e `fix/DTOSS-1234-fixed-a-bug`
- Pull request names should follow the same conventional commits standard as commits
- Write a bullet point description of the changes made in your PR
- Add a link to the ticket in the context section as well as any other context \
 you think would be helpfult for reviewers to know
- PR comments are to be marked resolved by the reviewer, not the author

 ## Making A New Function
1. Write the function & tests
2. Add [XML docs](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/)
3. Create a dockerfile and add to the relevant compose file
4. Add a health check
4. Update terraform (or ask the platform team to do it)