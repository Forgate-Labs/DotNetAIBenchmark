Feature: Cart discount
  Scenario: Discount starts at the threshold
    Given a cart subtotal of 100
    When the total is calculated
    Then the total should be 90
