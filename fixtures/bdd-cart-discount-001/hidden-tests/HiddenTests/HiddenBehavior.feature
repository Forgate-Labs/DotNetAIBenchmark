Feature: Cart discount hidden
  Scenario: No discount below threshold
    Given a cart subtotal of 99
    When the total is calculated
    Then the total should be 99
