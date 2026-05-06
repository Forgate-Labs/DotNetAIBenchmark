Feature: Password policy hidden
  Scenario: Long password with a digit is accepted
    Given the password abcdefg1
    When the password is validated
    Then it should be valid
