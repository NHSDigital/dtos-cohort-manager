# ADR-004: Postcode Validation

>|              |                                                                                                                                                                                    |
>| ------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
>| Date         | `26/02/2025`                                                                                                                              |
>| Status       | `Proposed`                        |
>| Deciders     | `Engineering` |
>| Significance | `Functional`                                                                                |
>| Owners       | `Will Larkin`                                                                                                                                                            |

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
It was measured agains a list of valid postcodes based on [all valid postcode patterns](https://ideal-postcodes.co.uk/guides/uk-postcode-format) and passed all of them, \
including postcodes with and without space separators

