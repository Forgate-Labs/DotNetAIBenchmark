Feature: Password policy
  Scenario: Long password without a digit is rejected
    Given the password abcdefghi
    When the password is validated
    Then it should be invalid
