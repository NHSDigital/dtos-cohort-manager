# ADR-004: Postcode Validation

>|              |                                                                                                                                                                                    |
>| ------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
>| Date         | `26/02/2025`                                                                                                                              |
>| Status       | `Proposed`                        |
>| Deciders     | `Engineering` |
>| Significance | `Functional`                                                                                |
>| Owners       | `Will Larkin`                                                                                                                                                            |

---

- [ADR-004: Postcode Validation](#ADR-004_Postcode_Validation.md)
  - [Context](#context)
  - [Decision](#decision)
    - [Assumptions](#assumptions)
    - [Drivers](#drivers)
    - [Options](#options)
    - [Outcome](#outcome)
    - [Rationale](#rationale)
  - [Consequences](#consequences)
  - [Compliance](#compliance)
  - [Notes](#notes)
  - [Actions](#actions)
  - [Tags](#tags)

## Context

Regex for validating/ parsing postcodes is needed at multiple points in the project. It was discovered that the existing regex we were using did not cover all scenarios. 

There is a lack of NHS/ government guidance on a standard way to validate the format of postcodes programatically so this ADR has been created to document \
the descison that has been made on how the project will validate postcodes.

## Decision

### Outcome
The follwoing regex will be used across the project to parse postcodes: \
`^([A-Za-z][A-Ha-hJ-Yj-y]?[0-9][A-Za-z0-9]? ?[0-9][A-Za-z]{2}|[Gg][Ii][Rr] ?0[Aa]{2})$`


### Rationale
[This solution from stackoverflow](https://stackoverflow.com/a/51885364) seemed to provide the most comprehensive regex \
It was measured using 

Provide a rationale for the decision that is based on weighing the options to ensure that the same questions are not going to be asked again and again unless the decision needs to be superseded.

For non-trivial decisions a comparison table can be useful for the reviewer. Decision criteria down one side, options across the top. You'll likely find decision criteria come from the Drivers section above. Effort can be an important driving factor.  You may have an intuitive feel for this, but reviewers will not. T-shirt sizing the effort for each option may help communicate.

## Consequences

Describe the resulting context, after applying the decision. All the identified consequences should be listed here, not just the positive ones. Any decision comes with many implications. For example, it may introduce a need to make other decisions as an effect of cross-cutting concerns; it may impact structural or operational characteristics of the software, and influence non-functional requirements; as a result, some things may become easier or more difficult to do because of this change. What are the trade-offs?

What are the conditions under which this decision no longer applies or becomes irrelevant?

## Compliance

Establish how the success is going to be measured. Once implemented, the effect might lend itself to be measured, therefore if appropriate a set of criteria for success could be established. Compliance checks of the decision can be manual or automated using a fitness function. If it is the latter this section can then specify how that fitness function would be implemented and whether there are any other changes to the codebase needed to measure this decision for compliance.

## Notes

Include any links to existing epics, decisions, dependencies, risks, and policies related to this decision record. This section could also include any further links to configuration items within the project or the codebase, signposting to the areas of change.

It is important that if the decision is sub-optimal or the choice is tactical or misaligned with the strategic directions the risk related to it is identified and clearly articulated. As a result of that, the expectation is that a [Tech Debt](./tech-debt.md) record is going to be created on the backlog.

## Actions

- [x] name, date by, action
- [ ] name, date by, action

## Tags

`#availability|#scalability|#elasticity|#performance|#reliability|#resilience|#maintainability|#testability|#deployability|#modularity|#simplicity|#security|#data|#cost|#usability|#accessibility|…` these tags are intended to be operational, structural or cross-cutting architecture characteristics to link to related decisions.
