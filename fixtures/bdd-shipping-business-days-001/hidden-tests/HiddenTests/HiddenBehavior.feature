Feature: Shipping business days hidden
  Scenario: Two business days after Friday is Tuesday
    Given the start date 2026-05-01
    When 2 business days are added
    Then the result should be 2026-05-05
