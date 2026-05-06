Feature: Shipping business days
  Scenario: One business day after Friday is Monday
    Given the start date 2026-05-01
    When 1 business days are added
    Then the result should be 2026-05-04
