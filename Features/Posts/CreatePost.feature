Feature: Create Post

  @smoke
  Scenario: Create a new post successfully
    Given I have a valid post payload
    When I send a POST request to /posts
    Then the response status code should be 201 Created
    And the response body should contain the created post data

  Scenario: Verify created post data
    Given I have a post payload with:
      | field  | value      |
      | userId | 1          |
      | title  | Test Title |
      | body   | Test Body  |
    When I send a POST request to /posts
    Then the response should contain:
      | field  | value      |
      | userId | 1          |
      | title  | Test Title |
      | body   | Test Body  |
